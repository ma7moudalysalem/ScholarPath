using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.Commands;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Application.Scholarships.Queries;

namespace ScholarPath.API.Controllers
{
    [ApiController]
    [Route("api/scholarships")] //  Convention route
    public class ScholarshipsController(IMediator mediator) : ControllerBase //  ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PaginatedList<ScholarshipDto>>> Get([FromQuery] GetScholarshipsQuery query)
        {
            //  Reading language from Accept-Language header
            var headerLang = Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault() ?? "en";
             var lang = headerLang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";

            var updatedQuery = query with { Language = lang };

            return await mediator.Send(updatedQuery);
        }
        

        [HttpGet("{id}")]
        public async Task<ActionResult<ScholarshipDetailDto>> GetById(Guid id, [FromQuery] string? language)
        {
            var headerValue = Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault() ?? "en";
            var detectedLang = headerValue. StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
            var lang = language ?? detectedLang;

            return await mediator.Send(new GetScholarshipByIdQuery(id, lang));
        }

        //  Post/Put/Delete methods with [Authorize] will be added here in the next step
        [HttpPost]
        [Authorize(Roles = "Company")] //  Authorization enforced
        public async Task<ActionResult<Guid>> Create(CreateScholarshipCommand command)
        {
            return await mediator.Send(command);
        }

        [HttpPost("{id}/bookmark")]
        [Authorize] // Any logged in user
        public async Task<ActionResult<bool>> ToggleBookmark(Guid id)
        {
            return await mediator.Send(new BookmarkToggleCommand(id));
        }
    }
}
