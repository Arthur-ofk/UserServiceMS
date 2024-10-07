using DAL.Abstraction;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<User>> GetUsersWithRolesAsync()
        {
            return await _context.Users
                .Include(u => u.Role) 
                .ToListAsync();
        }
        public async Task<User> GetUserWithRolesExplicitlyAsync(Guid id)
        {
            var user = await GetByIdAsync(id);
            if (user != null)
            {
                await _context.Entry(user).Reference(u => u.Role).LoadAsync(); 
            }
            return user;
        }
        public async Task<IEnumerable<User>> GetUsersByRoleAsync(string role)
        {
            return await _dbSet.Where(u => u.Role == role).ToListAsync();
        }
    }
}
