using System;
using System.Collections.Generic;
using System.Text;
using MediatR;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Commands.CreateApplication
{
    public record CreateApplicationCommand
(Guid ScholarshipId, string? PersonalNotes) : IRequest<ApplicationDto>;
}
