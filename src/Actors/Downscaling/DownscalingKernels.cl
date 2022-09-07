__constant sampler_t sampler = CLK_NORMALIZED_COORDS_FALSE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_NEAREST;

struct CLImageSize
{
   uint Width;
   uint Height;
};

__kernel void resize(__read_only image2d_t source, __write_only image2d_t dest, struct CLImageSize src_img_size, struct CLImageSize dst_img_size)
{
	const int gx = get_global_id(0);
    const int gy = get_global_id(1);
    const int2 pos = { gx, gy };
    float2 srcSize = (float2)(src_img_size.Width, src_img_size.Height);
    float2 dstSize = (float2)(dst_img_size.Width, dst_img_size.Height);
    float2 scale = srcSize / dstSize;
    float2 srcPosStart = convert_float2(pos) * scale;
    float2 srcPosEnd = convert_float2(pos + (int2)(1, 1)) * scale;
    float4 sum = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
    float weightSum = 0;    
    int2 inputStart = convert_int2((float2)(floor(srcPosStart.x), floor(srcPosStart.y)));   
    int2 inputEnd = convert_int2((float2)(ceil(srcPosEnd.x), (int)ceil(srcPosEnd.y)));
    
    float currentWeightY = 1.0f-srcPosStart.y+inputStart.y;
    for (int y = inputStart.y; y <= inputEnd.y; y++)
    {
        float currentWeightX = 1.0f-srcPosStart.x+inputStart.x;
        for (int x = inputStart.x; x <= inputEnd.x; x++)
        {            
            float weight = currentWeightX * currentWeightY;
            float4 srcValue = read_imagef(source, sampler, (int2)(x, y));
            sum += srcValue * weight;
            weightSum += weight;
            currentWeightX = x == inputEnd.x-1 ? srcPosEnd.x-inputEnd.x : 1.0f;   
        }

        currentWeightY = y == inputEnd.y-1 ? srcPosEnd.y-inputEnd.y : 1.0f;
    }
    
    write_imagef(dest, pos, sum/weightSum);
}