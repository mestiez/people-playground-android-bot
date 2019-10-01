using System;
using System.Collections.Generic;
using System.Text;

namespace AndroidBot
{
    public interface IPermissions
    {
        ulong[] Channels { get; }
        ulong[] Roles { get; }
        ulong[] Users { get; }
    }
}
