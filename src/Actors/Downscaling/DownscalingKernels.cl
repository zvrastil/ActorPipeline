__constant sampler_t sampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_NEAREST;

struct CLSourceRegion
{
   float Left;
   float Top;
   float Width;
   float Height;
};

struct CLImageSize
{
   uint Width;
   uint Height;
};

__kernel void resize(__read_only image2d_t source, __write_only image2d_t dest, struct CLSourceRegion src_img, struct CLImageSize dst_img_size)
{
	const int gx = get_global_id(0);
    const int gy = get_global_id(1);
    const int2 pos = { gx, gy };

}