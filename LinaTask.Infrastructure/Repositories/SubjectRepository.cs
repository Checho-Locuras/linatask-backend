using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class SubjectRepository : ISubjectRepository
    {
        private readonly LinaTaskDbContext _context;

        public SubjectRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Subject>> GetAllAsync()
        {
            return await _context.Subjects
                .Include(s => s.TeacherSubjects)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subject>> GetActivesAsync()
        {
            return await _context.Subjects
                .Where(s => s.IsActive)
                .Include(s => s.TeacherSubjects)
                .ToListAsync();
        }

        public async Task<IEnumerable<Subject>> GetByCategoryAsync(string category)
        {
            return await _context.Subjects
                .Where(s => s.Category == category && s.IsActive)
                .ToListAsync();
        }

        public async Task<Subject?> GetByIdAsync(Guid id)
        {
            return await _context.Subjects
                .Include(s => s.TeacherSubjects)
                .ThenInclude(ts => ts.Teacher)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Subject?> GetByNameAsync(string name)
        {
            return await _context.Subjects
                .FirstOrDefaultAsync(s => s.Name.ToLower() == name.ToLower());
        }

        public async Task<Subject> CreateAsync(Subject subject)
        {
            _context.Subjects.Add(subject);
            await _context.SaveChangesAsync();
            return subject;
        }

        public async Task<Subject> UpdateAsync(Subject subject)
        {
            _context.Subjects.Update(subject);
            await _context.SaveChangesAsync();
            return subject;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var subject = await GetByIdAsync(id);
            if (subject == null) return false;

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}