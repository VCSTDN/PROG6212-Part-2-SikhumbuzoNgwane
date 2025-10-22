using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using CMCSApp.Data;
using CMCSApp.Models;

namespace CMCSApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly long _fileSizeLimit = 5 * 1024 * 1024; // 5 MB
        private readonly string[] _permittedExtensions = new[] { ".pdf", ".docx", ".xlsx" };
        private readonly string _uploadFolder = "uploads"; // under wwwroot

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        public IActionResult Index() => View();

        public IActionResult LecturerClaim() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LecturerClaim(IFormFile supportDoc, string lecturerName, int hoursWorked, decimal hourlyRate, string notes)
        {
            if (string.IsNullOrWhiteSpace(lecturerName) || hoursWorked <= 0 || hourlyRate < 0)
            {
                ModelState.AddModelError(string.Empty, "Please provide valid lecturer name, hours and rate.");
                return View();
            }

            string savedFileName = null;

            if (supportDoc != null)
            {
                if (supportDoc.Length == 0 || supportDoc.Length > _fileSizeLimit)
                {
                    ModelState.AddModelError(string.Empty, "File is empty or exceeds 5 MB.");
                    return View();
                }

                var ext = Path.GetExtension(supportDoc.FileName).ToLowerInvariant();
                if (!_permittedExtensions.Contains(ext))
                {
                    ModelState.AddModelError(string.Empty, "Unsupported file type. Allowed: .pdf, .docx, .xlsx.");
                    return View();
                }

                var wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsPath = Path.Combine(wwwRoot, _uploadFolder);
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                var uniqueName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsPath, uniqueName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await supportDoc.CopyToAsync(stream);
                }

                savedFileName = uniqueName;
            }

            var claim = new Claim
            {
                LecturerName = lecturerName,
                HoursWorked = hoursWorked,
                HourlyRate = hourlyRate,
                Notes = notes,
                DocumentFileName = savedFileName,
                Status = "Pending",
                SubmittedAt = DateTime.UtcNow
            };

            await _db.Claims.AddAsync(claim);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Claim submitted successfully.";
            return RedirectToAction(nameof(ClaimStatus));
        }

        public IActionResult CoordinatorApproval()
        {
            var pending = _db.Claims.Where(c => c.Status == "Pending").OrderBy(c => c.SubmittedAt).ToList();
            return View(pending);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = "Approved";
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(CoordinatorApproval));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim != null)
            {
                claim.Status = "Rejected";
                await _db.SaveChangesAsync();
            }
            return RedirectToAction(nameof(CoordinatorApproval));
        }

        public IActionResult ClaimStatus()
        {
            var all = _db.Claims.OrderByDescending(c => c.SubmittedAt).ToList();
            return View(all);
        }
    }
}