using Domain.Authenticate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    public class UserService : IUserService
    {
        public bool IsValid(LoginRequestDTO req)
        {
            return true;
        }
    }
}
