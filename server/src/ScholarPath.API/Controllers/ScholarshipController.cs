using MediatR;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Application.Common.Exceptions;

namespace ScholarPath.API.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class ScholarshipsController : Controller

    {
        private readonly IMediator _mediator;
        public ScholarshipsController(IMediator mediator) => _mediator = mediator;

        [HttpGet]
        public async Task<ActionResult<PaginatedList<ScholarshipDto>>> Get([FromQuery] GetScholarshipsQuery query)
        {
            return await _mediator.Send(query);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScholarshipDetailDto>> GetById(Guid id)
        {
            return await _mediator.Send(new GetScholarshipByIdQuery(id));
        }
        
            
    }
}
    

