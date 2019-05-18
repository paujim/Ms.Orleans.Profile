using System;
using System.Threading.Tasks;
using Orleans;
using Profile.Core.Models;

namespace Profile.Interface
{
    public interface IBussinessMgntService : IGrainWithIntegerKey, IMgntService<Bussiness>
    {
    }
}
