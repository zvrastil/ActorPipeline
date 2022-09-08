__constant sampler_t sampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP | CLK_FILTER_NEAREST;

struct CLImageSize
{
   uint Width;
   uint Height;
};

struct CLImageRegion
{
   float Left;
   float Top;
   float Width;
   float Height;
};

__kernel void clear(__write_only image2d_t dest)
{
    const int2 pos = { get_global_id(0), get_global_id(1) };
    write_imagef(dest, pos, (float4)(0, 0, 0, 0));
}

__kernel void resize(__read_only image2d_t source, __write_only image2d_t dest, struct CLImageRegion src_image_region, struct CLImageSize dst_img_size)
{
	const int gx = get_global_id(0);
    const int gy = get_global_id(1);
    const int2 pos = { gx, gy };
    float2 srcOffset = (float2)(src_image_region.Left, src_image_region.Top);
    float2 srcSize = (float2)(src_image_region.Width, src_image_region.Height);
    float2 dstSize = (float2)(dst_img_size.Width, dst_img_size.Height);
    float2 scale = srcSize / dstSize;
    float2 srcPosStart = convert_float2(pos) * scale + srcOffset;
    float2 srcPosEnd = convert_float2(pos + (int2)(1, 1)) * scale + srcOffset;
    float4 sum = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
    float weightSum = 0;    
    int2 inputStart = convert_int2((float2)(floor(srcPosStart.x), floor(srcPosStart.y)));   
    int2 inputEnd = convert_int2((float2)(ceil(srcPosEnd.x), (int)ceil(srcPosEnd.y)));
    
    float currentWeightY = max(0.0f, 1.0f-srcPosStart.y+inputStart.y);
    for (int y = inputStart.y; y <= inputEnd.y; y++)
    {
        float currentWeightX = max(0.0f, 1.0f-srcPosStart.x+inputStart.x);
        for (int x = inputStart.x; x <= inputEnd.x; x++)
        {            
            float weight = currentWeightX * currentWeightY;
            float4 srcValue = read_imagef(source, sampler, (int2)(x, y));
            sum += srcValue * weight;
            weightSum += weight;
            currentWeightX = x == inputEnd.x-1 ? max(0.0f, srcPosEnd.x-inputEnd.x) : 1.0f;   
        }

        currentWeightY = y == inputEnd.y-1 ? max(0.0f, srcPosEnd.y-inputEnd.y) : 1.0f;
    }

    //if (gy == 0 && gx >= 90 && gx < 110  )
    //{
    //    printf("*** gx=%i sum=%.3v4hlf weight=%.3f\n", gx, sum, weightSum);
    //    printf("*** gx=%i pixel=%.3v4hlf\n", gx, sum/weightSum);
    //}
    
    write_imagef(dest, pos, sum/weightSum);
}