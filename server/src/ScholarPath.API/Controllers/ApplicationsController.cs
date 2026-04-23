using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Applications.Commands.CreateApplication;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.API.Controllers
{
    [Authorize(Roles = "student")]
    public class ApplicationsController : Controller
    {
        private readonly IMediator _mediator;

        public ApplicationsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public async Task<ActionResult<ApplicationDto>> Create(CreateApplicationCommand command)
        {
            // نستخدم المتغير المحقون _mediator
            return await _mediator.Send(command);
        }
    }
}
