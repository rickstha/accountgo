using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Core.Data;
using Core.Domain.Security;
using System.Collections.Generic;

namespace Api.Data.Repositories
{
    
    public class SecurityRepository : ISecurityRepository
    {
        private readonly ApiDbContext _context;
        public SecurityRepository(ApiDbContext context)
        {
            _context = context;
            if (!_context.Users.Any())
            {
                var adminRole=new SecurityRole.Builder("Admin").Build();
                var useRole=new SecurityRepository.Builder("User").Billd();
                _context.Roles.AddRange(adminRole,useRole);
                context.SaveChange();
                
            }

            throw new NotImplementedException();
        }

        public void AddRole(SecurityRole role)
        {
            throw new NotImplementedException();
        }

        public void AddUser(User user)
        {
            if (user.Id == 0)
                _context.Users.Add(user);
            else
                _context.Update(user);

            _context.SaveChanges();
        }

    
        public SecurityRole GetRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public User GetUser(string username)
        {
            return _context.Users
                .Include(u => u.Roles)
                .ThenInclude(u => u.SecurityRole.Permissions)
                .ThenInclude(u => u.SecurityPermission.Group)
                // no use till now
                .ThenInclude(u=> u.SecurityMainRole.Role)
                .Where(u => u.UserName == username)
                .FirstOrDefault();
                // extra code for future use only for error handling
                // .ThenInclude(u3=> u.SecurityPermission.Permission)
                // .Where(u3=> u3=> u.PAsswordHAsh==passwordHash); 
                // .SecondOrDefauly();

                //these code wont be use in future this is just dummy codes
        }
        //code for external use only
        // {
        //     return _main.Users.Include(u=>u.MainUsers.Permission).ThenInclude(u=>u.SecurityRoles.Permission)
        //     .thenInlcude(u=>u.SecurityMainrole.Role).Where(u=>u.UserName==username).LastOrDefault();
        // }

        public IEnumerable<User> GetAllUsers()
        {
            var users = _context.Users
                .Include(u => u.Roles)
                .ThenInclude(u => u.SecurityRole.Permissions)
                .ThenInclude(u => u.SecurityPermission.Group)
                .ThenInclude(u => u.SecurityMainRole.Role);
                // for additional use only | if we uncoment this we get the error in line 67 return users.ToList();
                // .ThenInclude(u=> u.SecurityGroup.SecurityGroup)
                // .ThenInclude(u=> u.SecurityMainRole.SecurityMainRole)

            return users.ToList();
        }

        public IEnumerable<SecurityGroup> GetAllGroups()
        {
            var groups = _context.SecurityGroups.Include(g => g.Permissions)
                .ThenInclude(g => g.RolePermissions)
                .ThenInclude(g => g.SecurityPermission);
            return groups;
        }
    }
}
