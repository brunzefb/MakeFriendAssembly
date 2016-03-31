// Guids.cs
// MUST match guids.h
using System;

namespace DreamWorks.MakeFriendAssembly
{
    static class GuidList
    {
        public const string guidMakeFriendAssemblyPkgString = "92bace77-fb0d-44b2-bc4c-943893fc18c1";
        public const string guidMakeFriendAssemblyCmdSetString = "59933e76-236c-4368-83cf-c819108f1c50";

        public static readonly Guid guidMakeFriendAssemblyCmdSet = new Guid(guidMakeFriendAssemblyCmdSetString);
    };
}