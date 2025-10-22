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
        public async Task<IActionResult> LecturerClaim(IFormFile supportDoc, string lecturerName, decimal hoursWorked, decimal hourlyRate, string notes)
        {
            if (string.IsNullOrWhiteSpace(lecturerName))
            {
                ModelState.AddModelError(nameof(lecturerName), "Lecturer name is required.");
            }
            if (hoursWorked <= 0m)
            {
                ModelState.AddModelError(nameof(hoursWorked), "Hours worked must be greater than 0.");
            }
            if (hourlyRate < 0m)
            {
                ModelState.AddModelError(nameof(hourlyRate), "Hourly rate cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                return View();
            }

            string savedFileName = null;

            if (supportDoc != null)
            {
                if (supportDoc.Length == 0 || supportDoc.Length > _fileSizeLimit)
                {
                    ModelState.AddModelError("supportDoc", "File is empty or exceeds 5 MB.");
                    return View();
                }

                var ext = Path.GetExtension(supportDoc.FileName).ToLowerInvariant();
                if (!_permittedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("supportDoc", "Unsupported file type. Allowed: .pdf, .docx, .xlsx.");
                    return View();
                }

                var wwwRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsPath = Path.Combine(wwwRoot, _uploadFolder);
                if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

                var uniqueName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(uploadsPath, uniqueName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await supportDoc.CopyToAsync(stream);
                    }
                    savedFileName = uniqueName;
                }
                catch
                {
                    ModelState.AddModelError("supportDoc", "An error occurred while saving the file.");
                    return View();
                }
            }

            var claim = new Claim
            {
                LecturerName = lecturerName.Trim(),
                HoursWorked = hoursWorked,
                HourlyRate = hourlyRate,
                Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
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
            var pending = _db.Claims
                .Where(c => c.Status == "Pending")
                .OrderBy(c => c.SubmittedAt)
                .ToList();
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

        [HttpPost]
        public async Task<JsonResult> ApproveAjax(int id)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim == null)
            {
                return Json(new { success = false, message = "Claim not found." });
            }

            claim.Status = "Approved";
            await _db.SaveChangesAsync();

            return Json(new { success = true, status = claim.Status });
        }

        [HttpPost]
        public async Task<JsonResult> RejectAjax(int id)
        {
            var claim = await _db.Claims.FindAsync(id);
            if (claim == null)
            {
                return Json(new { success = false, message = "Claim not found." });
            }

            claim.Status = "Rejected";
            await _db.SaveChangesAsync();

            return Json(new { success = true, status = claim.Status });
        }

        public IActionResult ClaimStatus()
        {
            var all = _db.Claims
                .OrderByDescending(c => c.SubmittedAt)
                .ToList();
            return View(all);
        }
    }
}