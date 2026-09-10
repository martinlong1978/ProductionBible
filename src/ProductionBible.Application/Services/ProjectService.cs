using Microsoft.EntityFrameworkCore;
using ProductionBible.Application.Data;
using ProductionBible.Application.Dtos;
using ProductionBible.Application.Entities;

namespace ProductionBible.Application.Services;

public class ProjectService : IProjectService
{
    private readonly ProductionBibleDbContext _db;

    public ProjectService(ProductionBibleDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProjectDto>> GetAllAsync()
    {
        return await _db.Projects
            .Select(p => new ProjectDto(p.Id, p.Name, p.Description))
            .ToListAsync();
    }

    public async Task<ProjectDto?> GetByIdAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        return project is null ? null : new ProjectDto(project.Id, project.Name, project.Description);
    }

    public async Task<ProjectDto> CreateAsync(CreateProjectRequest request)
    {
        var project = new Project { Name = request.Name, Description = request.Description };
        _db.Projects.Add(project);
        await _db.SaveChangesAsync();
        return new ProjectDto(project.Id, project.Name, project.Description);
    }

    public async Task<ProjectDto?> UpdateAsync(int id, UpdateProjectRequest request)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return null;

        project.Name = request.Name;
        project.Description = request.Description;
        await _db.SaveChangesAsync();
        return new ProjectDto(project.Id, project.Name, project.Description);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project is null) return false;

        _db.Projects.Remove(project);
        await _db.SaveChangesAsync();
        return true;
    }
}
