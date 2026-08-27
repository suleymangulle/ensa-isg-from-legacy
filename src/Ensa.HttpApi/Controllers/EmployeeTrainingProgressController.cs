using Ensa.Application.Contracts.Common;
using Ensa.Application.Contracts.Trainings;
using Ensa.Application.Contracts.Trainings.Dtos;
using Ensa.Application.Contracts.Trainings.Dtos.Navigations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ensa.HttpApi.Controllers;

/// <summary>
/// Remote-learning progress endpoints — <c>api/employee-training-progress</c>.
/// </summary>
public class EmployeeTrainingProgressController(IEmployeeTrainingProgressAppService appService) : EnsaController
{
    /// <summary>Returns one progress record.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<EmployeeTrainingProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmployeeTrainingProgressDto> GetAsync(int id, CancellationToken cancellationToken)
        => appService.GetAsync(id, cancellationToken);

    /// <summary>All progress records of one employee.</summary>
    [HttpGet("employee/{employeeId:int}")]
    [ProducesResponseType<ListResultDto<EmployeeTrainingProgressDto>>(StatusCodes.Status200OK)]
    public Task<ListResultDto<EmployeeTrainingProgressDto>> GetForEmployeeAsync(
        int employeeId,
        CancellationToken cancellationToken)
        => appService.GetForEmployeeAsync(employeeId, cancellationToken);

    /// <summary>Progress with the employee, the training and the remaining seconds.</summary>
    [HttpGet("{id:int}/detail")]
    [ProducesResponseType<EmployeeTrainingProgressNavigationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmployeeTrainingProgressNavigationDto> GetNavigationAsync(
        int id,
        CancellationToken cancellationToken)
        => appService.GetNavigationAsync(id, cancellationToken);

    /// <summary>Starts (or resumes) an employee's remote training.</summary>
    [HttpPost("start")]
    [ProducesResponseType<EmployeeTrainingProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmployeeTrainingProgressDto> StartAsync(
        [FromBody] StartTrainingProgressDto input,
        CancellationToken cancellationToken)
        => appService.StartAsync(input, cancellationToken);

    /// <summary>Records elapsed time and the current page.</summary>
    [HttpPut("{id:int}/topic-progress")]
    [ProducesResponseType<EmployeeTrainingProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmployeeTrainingProgressDto> SaveTopicProgressAsync(
        int id,
        [FromBody] SaveTopicProgressDto input,
        CancellationToken cancellationToken)
        => appService.SaveTopicProgressAsync(id, input, cancellationToken);

    /// <summary>Records an exam attempt with its score.</summary>
    [HttpPost("{id:int}/exam")]
    [ProducesResponseType<EmployeeTrainingProgressDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<EmployeeTrainingProgressDto> SubmitExamAsync(
        int id,
        [FromBody] SubmitExamDto input,
        CancellationToken cancellationToken)
        => appService.SubmitExamAsync(id, input, cancellationToken);
    /// <summary>
    /// Progress across employees, paged and filterable — the "who has not finished" view.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<PagedResultDto<EmployeeTrainingProgressListDto>>(StatusCodes.Status200OK)]
    public Task<PagedResultDto<EmployeeTrainingProgressListDto>> GetListAsync(
        [FromQuery] GetEmployeeTrainingProgressListInput input,
        CancellationToken cancellationToken)
        => appService.GetListAsync(input, cancellationToken);
}
