using CafeDuaHau.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using CafeDuaHau.Data;
using CafeDuaHau.Models;
using Microsoft.AspNetCore.Mvc;

namespace CafeDuaHau.Controllers;

public class MenuController(CafeDbContext database) : Controller
{
    // GET /  hoặc  /Menu?category=ca-phe&q=sữa
    public IActionResult Index(string? category, string? q)
    {
        var active = string.IsNullOrWhiteSpace(category) ? "all" : category;
        var keyword = q?.Trim() ?? "";

        IEnumerable<MenuItem> items = database.MenuItems.AsNoTracking().ToList();

        if (active != "all")
            items = items.Where(i => i.CategoryId == active);

        if (keyword.Length > 0)
            items = items.Where(i => i.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        var model = new MenuIndexViewModel
        {
            Categories = database.Categories.AsNoTracking().OrderBy(category => category.Name).ToList(),
            Items = items.ToList(),
            ActiveCategory = active,
            Keyword = keyword
        };

        return View(model);
    }

    public IActionResult Details(string id)
    {
        var item = database.MenuItems.AsNoTracking().SingleOrDefault(item => item.Id == id);
        if (item is null)
            return NotFound();

        ViewData["CategoryName"] = database.Categories.Where(category => category.Id == item.CategoryId)
            .Select(category => category.Name).SingleOrDefault();

        var photoIds = database.MenuPhotos
            .Where(photo => photo.MenuItemId == id)
            .OrderBy(photo => photo.Position).ThenBy(photo => photo.Id)
            .Select(photo => photo.Id).ToList();
        var photos = photoIds.Select(photoId => Url.Action(nameof(Photo), new { id = photoId })!).ToList();
        if (photos.Count == 0)
            photos.Add(item.ImageUrl);

        var reviews =
            (from review in database.MenuReviews
             join customer in database.CustomerAccounts on review.CustomerAccountId equals customer.Id
             where review.MenuItemId == id
             orderby review.CreatedAt descending
             select new MenuReviewEntry(customer.UserName, review.Rating, review.Comment, review.CreatedAt))
            .ToList();
        var canReview = User.IsInRole("Customer") && CanReview(id, CustomerIdentity.GetId(User));
        return View(new MenuDetailsViewModel(item, photos, reviews, canReview));
    }

    [HttpGet]
    public IActionResult Photo(Guid id)
    {
        var photo = database.MenuPhotos.AsNoTracking().SingleOrDefault(photo => photo.Id == id);
        if (photo is null)
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers.CacheControl = "public, max-age=86400";
        return File(photo.Content, photo.ContentType);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public IActionResult Review(string id, MenuReviewForm form, bool fromOrders = false)
    {
        if (!database.MenuItems.Any(item => item.Id == id))
            return NotFound();

        var customerId = CustomerIdentity.GetId(User);
        if (!CanReview(id, customerId))
            TempData["Error"] = "Bạn chỉ có thể đánh giá một lần cho mỗi món đã nhận.";
        else if (!ModelState.IsValid)
            TempData["Error"] = ModelState.Values.SelectMany(value => value.Errors).First().ErrorMessage;
        else
        {
            database.MenuReviews.Add(new MenuReview
            {
                MenuItemId = id,
                CustomerAccountId = customerId,
                Rating = form.Rating,
                Comment = form.Comment.Trim(),
                CreatedAt = DateTime.Now
            });
            try
            {
                database.SaveChanges();
                TempData["Success"] = "Đã gửi đánh giá.";
            }
            catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
            {
                TempData["Error"] = "Bạn đã đánh giá món này.";
            }
        }

        return fromOrders
            ? RedirectToAction("Index", "Orders")
            : RedirectToAction(nameof(Details), controllerName: null, routeValues: new { id }, fragment: "reviews");
    }

    private bool CanReview(string itemId, Guid customerId) =>
        database.Orders.Any(order => order.CustomerAccountId == customerId &&
            order.Status == OrderStatus.Delivered && order.Items.Any(item => item.ItemId == itemId)) &&
        !database.MenuReviews.Any(review => review.CustomerAccountId == customerId && review.MenuItemId == itemId);
}
