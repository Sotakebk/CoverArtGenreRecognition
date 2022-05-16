using System;

namespace DataAcquisition.Shared.Structures
{
    public struct Release
    {
        public Guid Guid;
        public int GroupId;

        public Release(Guid guid, int groupId)
        {
            Guid = guid;
            GroupId = groupId;
        }
    }
}
