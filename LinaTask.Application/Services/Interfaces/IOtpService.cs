using System;
using System.Collections.Generic;
using System.Text;

namespace LinaTask.Domain.Interfaces
{
    public interface IOtpService
    {
        string GenerateOtp(int length = 6);
        bool ValidateOtp(string providedOtp, string storedOtp);
    }
}
