using CafeDuaHau.Data;
using CafeDuaHau.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace CafeDuaHau.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(CafeDbContext database) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login() => RedirectToAction("Login", "Account");

    [HttpGet]
    public IActionResult Index(string? q, string? category, int page = 1)
    {
        const int pageSize = 10;
        var keyword = q?.Trim();
        var items = database.MenuItems.AsNoTracking();

        if (!string.IsNullOrEmpty(keyword))
            items = items.Where(item => item.Name.Contains(keyword));
        if (!string.IsNullOrEmpty(category))
            items = items.Where(item => item.CategoryId == category);

        var total = items.Count();
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, pageCount);
        ViewData["Keyword"] = keyword;
        ViewData["Category"] = category;
        ViewData["Page"] = page;
        ViewData["Total"] = total;
        ViewData["PageCount"] = pageCount;

        LoadCategories();
        ViewData["StockItems"] = database.MenuItems.AsNoTracking().OrderBy(item => item.Name).ToList();

        return View(items.OrderBy(item => item.CategoryId).ThenBy(item => item.Name).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList());
    }

    [HttpGet]
    public IActionResult Edit(string? id)
    {
        if (id is null)
            return MenuFormView(new AdminMenuForm());

        var item = database.MenuItems.AsNoTracking().SingleOrDefault(item => item.Id == id);
        if (item is null)
            return NotFound();

        ViewData["ItemId"] = id;
        ViewData["SavedPhotos"] = GetSavedPhotos(item);
        return MenuFormView(new AdminMenuForm
        {
            Name = item.Name,
            Description = item.Description,
            CategoryId = item.CategoryId,
            Price = item.HasSizes ? null : item.Price,
            MediumPrice = item.Sizes.SingleOrDefault(size => size.Label == "M")?.Price,
            LargePrice = item.Sizes.SingleOrDefault(size => size.Label == "L")?.Price,
            ExtraLargePrice = item.Sizes.SingleOrDefault(size => size.Label == "XL")?.Price
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string? id, AdminMenuForm form, CancellationToken cancellationToken)
    {
        var categoryId = form.CategoryId;
        Category? newCategory = null;
        if (categoryId == AdminMenuForm.NewCategoryOption)
        {
            var name = form.NewCategoryName?.Trim();
            if (string.IsNullOrEmpty(name) || name.Length > 80)
                ModelState.AddModelError(nameof(form.NewCategoryName), "Nhập tên danh mục từ 1 đến 80 ký tự.");
            else
            {
                var existing = await database.Categories.AsNoTracking()
                    .SingleOrDefaultAsync(category => category.Name == name, cancellationToken);
                if (existing != null)
                    categoryId = existing.Id;
                else
                {
                    newCategory = new Category { Id = Guid.NewGuid().ToString("N"), Name = name };
                    categoryId = newCategory.Id;
                }
            }
        }
        else if (!await database.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken))
            ModelState.AddModelError(nameof(form.CategoryId), "Danh mục không hợp lệ.");

        var hasSizes = form.MediumPrice.HasValue || form.LargePrice.HasValue || form.ExtraLargePrice.HasValue;
        if (form.Price.HasValue == hasSizes)
            ModelState.AddModelError("", "Nhập giá chung hoặc giá theo size M/L.");

        if (hasSizes && !form.MediumPrice.HasValue)
            ModelState.AddModelError(nameof(form.MediumPrice), "Vui lòng nhập giá size M để làm giá mặc định.");

        ViewData["ItemId"] = id;
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var item = id is null
            ? new MenuItem { Id = Guid.NewGuid().ToString("N"), ImageUrl = MenuData.PlaceholderImage, StockQuantity = 0, IsSoldOut = true }
            : FindMenuItemForPhotoUpdate(id);
        if (item is null)
            return NotFound();

        var savedPhotos = GetSavedPhotos(item);
        ViewData["SavedPhotos"] = savedPhotos;
        if (!ModelState.IsValid)
            return MenuFormView(form);

        var nextPosition = await database.MenuPhotos.Where(photo => photo.MenuItemId == item.Id)
            .Select(photo => (int?)photo.Position).MaxAsync(cancellationToken) ?? 0;
        if (form.Photos.Count > 5 || savedPhotos.Count(photo => photo.Id.HasValue) + form.Photos.Count > 8)
        {
            ModelState.AddModelError(nameof(form.Photos), "Chọn tối đa 5 ảnh mỗi lần và 8 ảnh cho mỗi món.");
            return MenuFormView(form);
        }

        var photos = new List<MenuPhoto>();
        foreach (var upload in form.Photos)
        {
            if (upload.Length is <= 0 or > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(form.Photos), "Mỗi ảnh phải nhỏ hơn hoặc bằng 5 MB.");
                return MenuFormView(form);
            }

            using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var type = MenuPhoto.DetectContentType(bytes);
            if (type is null)
            {
                ModelState.AddModelError(nameof(form.Photos), "Chỉ nhận ảnh JPG, PNG hoặc WebP.");
                return MenuFormView(form);
            }

            photos.Add(new MenuPhoto
            {
                MenuItemId = item.Id,
                Position = ++nextPosition,
                ContentType = type,
                Content = bytes
            });
        }

        if (photos.Count > 0 && photos[0].Position == 1)
            item.ImageUrl = Url.Action("Photo", "Menu", new { id = photos[0].Id })!;

        item.Name = form.Name.Trim();
        item.Description = form.Description?.Trim() ?? "";
        item.CategoryId = categoryId;
        item.Price = form.Price ?? 0;
        SetSize(item, "M", form.MediumPrice);
        SetSize(item, "L", form.LargePrice);
        SetSize(item, "XL", form.ExtraLargePrice);
        if (id is null)
            database.MenuItems.Add(item);

        if (newCategory != null)
            database.Categories.Add(newCategory);
        database.MenuPhotos.AddRange(photos);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (newCategory != null &&
            error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await transaction.RollbackAsync(cancellationToken);
            ModelState.AddModelError(nameof(form.NewCategoryName), "Danh mục vừa được thêm. Vui lòng chọn danh mục đó và lưu lại.");
            return MenuFormView(form);
        }
        TempData["Success"] = "Đã lưu món.";
        return RedirectToAction(nameof(Index));
    }

    private void LoadCategories() =>
        ViewData["Categories"] = database.Categories.AsNoTracking().OrderBy(category => category.Name).ToList();

    private IActionResult MenuFormView(AdminMenuForm form)
    {
        LoadCategories();
        if (Request.Headers["X-Menu-Dialog"] == "1")
        {
            ViewData["MenuDialog"] = true;
            return PartialView("_MenuForm", form);
        }

        return View("Edit", form);
    }

    [HttpPost]
    public IActionResult SetStock(string id, int? originalQuantity, int? quantity)
    {
        if (!ModelState.IsValid || quantity is null or < 0)
        {
            TempData["StockMessage"] = "Vui lòng nhập số lượng nguyên từ 0 trở lên.";
        }
        else
        {
            var updated = database.MenuItems
                .Where(item => item.Id == id && item.StockQuantity == originalQuantity)
                .ExecuteUpdate(update => update
                    .SetProperty(item => item.StockQuantity, quantity)
                    .SetProperty(item => item.IsSoldOut, quantity == 0));
            if (updated == 1)
                TempData["StockMessage"] = "Đã lưu số lượng tồn kho.";
            else
                TempData["StockMessage"] = "Tồn kho vừa thay đổi hoặc món đã bị xóa. Vui lòng kiểm tra lại.";
        }

        return RedirectToAction(nameof(Index), controllerName: null, routeValues: null, fragment: "stock-dialog");
    }

    [HttpPost]
    public IActionResult SetPromotion(string id, bool isHot, bool hasDiscount, int? discountPercent)
    {
        if (!ModelState.IsValid || (hasDiscount && discountPercent is null or < 1 or > 100))
        {
            TempData["Error"] = "Nhập mức giảm từ 1 đến 100%.";
            return RedirectToAction(nameof(Index));
        }

        var discount = hasDiscount ? discountPercent : null;
        var updated = database.MenuItems.Where(item => item.Id == id)
            .ExecuteUpdate(update => update
                .SetProperty(item => item.IsHot, isHot)
                .SetProperty(item => item.DiscountPercent, discount));
        if (updated == 0)
            return NotFound();

        TempData["Success"] = "Đã lưu khuyến mãi của món.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Delete(string id)
    {
        var item = database.MenuItems.SingleOrDefault(item => item.Id == id);
        if (item is null)
            return NotFound();

        database.MenuItems.Remove(item);
        database.SaveChanges();
        TempData["Success"] = "Đã xóa món.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult DeletePhoto(string id, Guid? photoId)
    {
        if (!ModelState.IsValid)
            return BadRequest();

        using var transaction = database.Database.BeginTransaction();
        var item = FindMenuItemForPhotoUpdate(id);
        if (item is null)
            return NotFound();

        var photos = GetSavedPhotos(item);
        var photo = photos.Find(entry => entry.Id == photoId);
        if (photo is null)
            return NotFound();

        if (photo.Id is Guid uploadedId)
            database.MenuPhotos.Remove(new MenuPhoto { Id = uploadedId });
        if (photo.IsCover)
            item.ImageUrl = photos.FirstOrDefault(entry => entry.Id != photoId)?.Url ?? MenuData.PlaceholderImage;

        database.SaveChanges();
        transaction.Commit();
        TempData["Success"] = "Đã xóa ảnh.";
        return RedirectToAction(nameof(Edit), controllerName: null, routeValues: new { id }, fragment: "edit-photos");
    }

    // Serialize photo changes for the same menu item so the cover always points to a saved photo.
    private MenuItem? FindMenuItemForPhotoUpdate(string id) =>
        database.MenuItems.FromSqlInterpolated($"SELECT * FROM dbo.MenuItems WITH (UPDLOCK) WHERE Id = {id}")
            .SingleOrDefault();

    private List<AdminMenuPhoto> GetSavedPhotos(MenuItem item)
    {
        var ids = database.MenuPhotos.Where(photo => photo.MenuItemId == item.Id)
            .OrderBy(photo => photo.Position).ThenBy(photo => photo.Id)
            .Select(photo => photo.Id).ToList();

        if (ids.Count == 0 && !string.IsNullOrWhiteSpace(item.ImageUrl) && item.ImageUrl != MenuData.PlaceholderImage)
            return [new AdminMenuPhoto(null, item.ImageUrl, true)];

        return ids.Select((id, index) =>
            new AdminMenuPhoto(id, Url.Action("Photo", "Menu", new { id })!, index == 0)).ToList();
    }

    private static void SetSize(MenuItem item, string label, int? price)
    {
        var size = item.Sizes.SingleOrDefault(size => size.Label == label);
        if (price is null)
        {
            if (size is not null)
                item.Sizes.Remove(size);
        }
        else if (size is null)
            item.Sizes.Add(new MenuSize { Label = label, Price = price.Value });
        else
            size.Price = price.Value;
    }
}
