using CafeDuaHau.Data;
using CafeDuaHau.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        return View(items.OrderBy(item => item.CategoryId).ThenBy(item => item.Name).ThenBy(item => item.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToList());
    }

    [HttpGet]
    public IActionResult Edit(string? id)
    {
        if (id is null)
            return View(new AdminMenuForm());

        var item = database.MenuItems.AsNoTracking().SingleOrDefault(item => item.Id == id);
        if (item is null)
            return NotFound();

        ViewData["ItemId"] = id;
        ViewData["SavedPhotos"] = GetSavedPhotos(item);
        return View(new AdminMenuForm
        {
            Name = item.Name,
            Description = item.Description,
            CategoryId = item.CategoryId,
            Price = item.HasSizes ? null : item.Price,
            MediumPrice = item.Sizes.SingleOrDefault(size => size.Label == "M")?.Price,
            LargePrice = item.Sizes.SingleOrDefault(size => size.Label == "L")?.Price,
            IsSoldOut = item.IsSoldOut
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(string? id, AdminMenuForm form, CancellationToken cancellationToken)
    {
        if (!MenuData.Categories.Any(category => category.Id == form.CategoryId))
            ModelState.AddModelError(nameof(form.CategoryId), "Danh mục không hợp lệ.");

        var hasSizes = form.MediumPrice.HasValue || form.LargePrice.HasValue;
        if (form.Price.HasValue == hasSizes)
            ModelState.AddModelError("", "Nhập giá chung hoặc giá theo size M/L.");

        ViewData["ItemId"] = id;
        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        var item = id is null
            ? new MenuItem { Id = Guid.NewGuid().ToString("N"), ImageUrl = MenuData.PlaceholderImage }
            : FindMenuItemForPhotoUpdate(id);
        if (item is null)
            return NotFound();

        var savedPhotos = GetSavedPhotos(item);
        ViewData["SavedPhotos"] = savedPhotos;
        if (!ModelState.IsValid)
            return View(form);

        var nextPosition = await database.MenuPhotos.Where(photo => photo.MenuItemId == item.Id)
            .Select(photo => (int?)photo.Position).MaxAsync(cancellationToken) ?? 0;
        if (form.Photos.Count > 5 || savedPhotos.Count(photo => photo.Id.HasValue) + form.Photos.Count > 8)
        {
            ModelState.AddModelError(nameof(form.Photos), "Chọn tối đa 5 ảnh mỗi lần và 8 ảnh cho mỗi món.");
            return View(form);
        }

        var photos = new List<MenuPhoto>();
        foreach (var upload in form.Photos)
        {
            if (upload.Length is <= 0 or > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(form.Photos), "Mỗi ảnh phải nhỏ hơn hoặc bằng 5 MB.");
                return View(form);
            }

            using var buffer = new MemoryStream((int)upload.Length);
            await upload.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            var type = MenuPhoto.DetectContentType(bytes);
            if (type is null)
            {
                ModelState.AddModelError(nameof(form.Photos), "Chỉ nhận ảnh JPG, PNG hoặc WebP.");
                return View(form);
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
        item.CategoryId = form.CategoryId;
        item.Price = form.Price ?? 0;
        item.IsSoldOut = form.IsSoldOut;
        SetSize(item, "M", form.MediumPrice);
        SetSize(item, "L", form.LargePrice);
        if (id is null)
            database.MenuItems.Add(item);

        database.MenuPhotos.AddRange(photos);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["Success"] = "Đã lưu món.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult SetAvailability(string id, bool? isSoldOut)
    {
        if (!ModelState.IsValid || !isSoldOut.HasValue)
            return BadRequest();

        var updated = database.MenuItems.Where(item => item.Id == id)
            .ExecuteUpdate(update => update.SetProperty(item => item.IsSoldOut, isSoldOut.Value));
        if (updated == 0)
            return NotFound();

        TempData["Success"] = isSoldOut.Value ? "Đã đánh dấu hết hàng." : "Đã đánh dấu còn hàng.";
        return RedirectToAction(nameof(Index));
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
