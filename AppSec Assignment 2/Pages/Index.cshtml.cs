using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using AppSec_Assignment_2.Data;
using AppSec_Assignment_2.Models;
using AppSec_Assignment_2.Services;

namespace AppSec_Assignment_2.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly ApplicationDbContext _context;
        private readonly MemberProtectionService _memberProtectionService;

        public IndexModel(
            ILogger<IndexModel> logger,
            ApplicationDbContext context,
            MemberProtectionService memberProtectionService)
        {
            _logger = logger;
            _context = context;
            _memberProtectionService = memberProtectionService;
        }

        public Member? Member { get; set; }
        public string? DecryptedNric { get; set; }

        public async Task OnGetAsync()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var memberIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (int.TryParse(memberIdClaim, out int memberId))
                {
                    Member = await _context.Members
                        .AsNoTracking()
                        .FirstOrDefaultAsync(m => m.Id == memberId);

                    if (Member != null)
                    {
                        // Decrypt NRIC for display
                        DecryptedNric = _memberProtectionService.UnprotectNric(Member.Nric);
                    }
                }
            }
        }
    }
}
