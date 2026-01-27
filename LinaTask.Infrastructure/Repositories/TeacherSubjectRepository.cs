using LinaTask.Domain.Interfaces;
using LinaTask.Domain.Models;
using LinaTask.Infrastructure.DataBaseContext;
using Microsoft.EntityFrameworkCore;

namespace LinaTask.Infrastructure.Repositories
{
    public class TeacherSubjectRepository : ITeacherSubjectRepository
    {
        private readonly LinaTaskDbContext _context;

        public TeacherSubjectRepository(LinaTaskDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TeacherSubject>> GetAllAsync()
        {
            return await _context.TeacherSubjects
                .Include(ts => ts.Teacher)
                .Include(ts => ts.Subject)
                .ToListAsync();
        }

        public async Task<IEnumerable<TeacherSubject>> GetByTeacherIdAsync(Guid teacherId)
        {
            return await _context.TeacherSubjects
                .Include(ts => ts.Subject)
                .Where(ts => ts.TeacherId == teacherId)
                .ToListAsync();
        }

        public async Task<IEnumerable<TeacherSubject>> GetBySubjectIdAsync(Guid subjectId)
        {
            return await _context.TeacherSubjects
                .Include(ts => ts.Teacher)
                .Where(ts => ts.SubjectId == subjectId)
                .ToListAsync();
        }

        public async Task<TeacherSubject?> GetByIdAsync(Guid id)
        {
            return await _context.TeacherSubjects
                .Include(ts => ts.Teacher)
                .Include(ts => ts.Subject)
                .FirstOrDefaultAsync(ts => ts.Id == id);
        }

        public async Task<TeacherSubject> CreateAsync(TeacherSubject teacherSubject)
        {
            _context.TeacherSubjects.Add(teacherSubject);
            await _context.SaveChangesAsync();
            return teacherSubject;
        }

        public async Task<TeacherSubject> UpdateAsync(TeacherSubject teacherSubject)
        {
            _context.TeacherSubjects.Update(teacherSubject);
            await _context.SaveChangesAsync();
            return teacherSubject;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var teacherSubject = await GetByIdAsync(id);
            if (teacherSubject == null) return false;

            _context.TeacherSubjects.Remove(teacherSubject);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(Guid teacherId, Guid subjectId)
        {
            return await _context.TeacherSubjects
                .AnyAsync(ts => ts.TeacherId == teacherId && ts.SubjectId == subjectId);
        }
    }
}