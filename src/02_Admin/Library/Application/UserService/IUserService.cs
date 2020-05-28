using Domain.Authenticate;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application
{
    public interface IUserService
    {
        bool IsValid(LoginRequestDTO req);
    }
}
