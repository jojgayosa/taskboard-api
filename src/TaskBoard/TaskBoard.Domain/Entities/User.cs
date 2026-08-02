using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskBoard.Domain.Common;

namespace TaskBoard.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; private set; } = string.Empty;
        public string Email { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;

        private readonly List<ProjectMember> _projectMemberships = [];
        public IReadOnlyCollection<ProjectMember> ProjectMemberships => _projectMemberships.AsReadOnly();

        private User() { }

        public static User Create(string username, string email, string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required", nameof(username));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("A valid email is required", nameof(email));

            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("Password hash is required", nameof(passwordHash));

            return new User
            {
                Id = Guid.NewGuid(),
                Username = username,
                Email = email,
                PasswordHash = passwordHash
            };
        }

        public void ChangePassword(string newPasswrodHash)
        {
            if (string.IsNullOrWhiteSpace(newPasswrodHash))
                throw new ArgumentException("Password hash is required", nameof(newPasswrodHash));

            PasswordHash = newPasswrodHash;
        }

        public void UpdateProfile(string username, string email)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Username is required", nameof(username));

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("A valid email is required", nameof(email));

            Username = username;
            Email = email;
        }
    }

}
