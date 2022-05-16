namespace DataAcquisition.Shared.Structures
{
    public struct CoverArt
    {
        public long ImageId;
        public ImageFileType FileType;

        public CoverArt(long imageId, ImageFileType fileType)
        {
            ImageId = imageId;
            FileType = fileType;
        }
    }
}
