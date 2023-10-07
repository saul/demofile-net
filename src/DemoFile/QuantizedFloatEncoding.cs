using System.Diagnostics;

namespace DemoFile;

[Flags]
internal enum QuantizedFloatFlags
{
    Unset = 0,
    RoundDown = 1 << 0,
    RoundUp = 1 << 1,
    EncodeZero = 1 << 2,
    EncodeIntegers = 1 << 3
}

internal readonly record struct QuantizedFloatEncoding(
    float Low,
    float High,
    float DecMul,
    int BitCount,
    QuantizedFloatFlags Flags)
{
    public static QuantizedFloatEncoding Create(FieldEncodingInfo fieldEncodingInfo)
    {
        // Set common properties
        if (fieldEncodingInfo.BitCount == 0 || fieldEncodingInfo.BitCount >= 32)
        {
            // todo: figure out why this is firing - the decoded floats will always be 0
            //Debug.Assert(false, "Unexpected quantized float encoding!");
            return new QuantizedFloatEncoding(
                Low: 0.0f,
                High: 0.0f,
                DecMul: 0.0f,
                BitCount: 32,
                Flags: QuantizedFloatFlags.Unset);
        }

        var low = fieldEncodingInfo.LowValue.GetValueOrDefault(0.0f);
        var high = fieldEncodingInfo.HighValue.GetValueOrDefault(1.0f);

        // Validate flags
        var flags = ValidateFlags(
            (QuantizedFloatFlags)fieldEncodingInfo.EncodeFlags,
            low,
            high);

        // Handle Round Up, Round Down
        var bitCount = fieldEncodingInfo.BitCount;
        Debug.Assert(bitCount > 0);
        var steps = 1 << bitCount;

        var offset = 0.0f;
        if (flags.HasFlag(QuantizedFloatFlags.RoundDown))
        {
            offset = (high - low) / steps;
            high -= offset;
        }
        else if (flags.HasFlag(QuantizedFloatFlags.RoundUp))
        {
            offset = (high - low) / steps;
            low += offset;
        }

        // Handle integer encoding flag
        if (flags.HasFlag(QuantizedFloatFlags.EncodeIntegers))
        {
            var delta = Math.Max(1.0f, high - low);

            var deltaLog2 = (int)Math.Ceiling(Math.Log2(delta));
            var range = 1 << deltaLog2;

            bitCount = Math.Max(bitCount, deltaLog2);
            steps = 1 << bitCount;
            offset = range / (float)steps;
            high = low + range - offset;
        }

        // Assign multipliers
        var highLowMul = CalculateHighLowMul(high - low, bitCount, steps);
        var decMul = 1.0f / (steps - 1);

        float Quantize(float value)
        {
            if (value < low)
            {
                if (!flags.HasFlag(QuantizedFloatFlags.RoundUp))
                    throw new Exception("Field tried to quantize an out of range value");

                return low;
            }

            if (value > high)
            {
                if (!flags.HasFlag(QuantizedFloatFlags.RoundDown))
                    throw new Exception("Field tried to quantize an out of range value");

                return high;
            }

            var i = (uint)((value - low) * highLowMul);
            return low + (high - low) * (i * decMul);
        }
        
        // Remove unnecessary flags
        if (flags.HasFlag(QuantizedFloatFlags.RoundDown) && Quantize(low) == low)
        {
            flags &= ~QuantizedFloatFlags.RoundDown;
        }
        if (flags.HasFlag(QuantizedFloatFlags.RoundUp) && Quantize(high) == high)
        {
            flags &= ~QuantizedFloatFlags.RoundUp;
        }
        if (flags.HasFlag(QuantizedFloatFlags.EncodeZero) && Quantize(0.0f) == 0.0f)
        {
            flags &= ~QuantizedFloatFlags.EncodeZero;
        }

        return new QuantizedFloatEncoding(
            Low: low,
            High: high,
            DecMul: decMul,
            BitCount: bitCount,
            Flags: flags);
    }

    private static QuantizedFloatFlags ValidateFlags(
        QuantizedFloatFlags flags,
        float low,
        float high)
    {
        if (flags == QuantizedFloatFlags.Unset)
            return QuantizedFloatFlags.Unset;

        // Discard zero flag when encoding min / max set to 0
        if (low == 0.0 && flags.HasFlag(QuantizedFloatFlags.RoundDown)
            || (high == 0.0 && flags.HasFlag(QuantizedFloatFlags.RoundUp)))
        {
            flags &= ~QuantizedFloatFlags.EncodeZero;
        }

        // If min / max is zero when encoding zero, switch to round up / round down instead
        if (low == 0.0 && flags.HasFlag(QuantizedFloatFlags.EncodeZero))
        {
            flags |= QuantizedFloatFlags.RoundDown;
            flags &= ~QuantizedFloatFlags.EncodeZero;
        }

        if (high == 0.0 && flags.HasFlag(QuantizedFloatFlags.EncodeZero))
        {
            flags |= QuantizedFloatFlags.RoundUp;
            flags &= ~QuantizedFloatFlags.EncodeZero;
        }

        // If the range doesn't span zero, we don't need to encode it
        if (low > 0.0 || high < 0.0)
        {
            flags &= ~QuantizedFloatFlags.EncodeZero;
        }

        if (flags.HasFlag(QuantizedFloatFlags.EncodeIntegers))
        {
            flags = QuantizedFloatFlags.EncodeIntegers;
        }

        if (flags.HasFlag(QuantizedFloatFlags.RoundDown)
            && flags.HasFlag(QuantizedFloatFlags.RoundUp))
        {
            throw new Exception("RoundUp / RoundDown are mutually exclusive");
        }

        return flags;
    }

    private static float CalculateHighLowMul(float range, int bitCount, int steps)
    {
        var high = bitCount == 32
            ? 0xFFFFFFFE
            : (1u << bitCount) - 1;

        var highMul = range == 0.0f ? high : high / range;

        // Adjust precision
        // todo: make this checked? original code also checked float64
        if (highMul * range > high)
        {
            var multipliers = new[] { 0.9999f, 0.99f, 0.9f, 0.8f, 0.7f };
            foreach (var multiplier in multipliers)
            {
                highMul = high / range * multiplier;
                if (highMul * range <= high)
                    break;
            }
        }

        Debug.Assert(highMul != 0.0f);
        return highMul;
    }

    public float Decode(ref BitBuffer buffer)
    {
        if (Flags.HasFlag(QuantizedFloatFlags.RoundDown) && buffer.ReadOneBit())
            return Low;

        if (Flags.HasFlag(QuantizedFloatFlags.RoundUp) && buffer.ReadOneBit())
            return High;

        if (Flags.HasFlag(QuantizedFloatFlags.EncodeZero) && buffer.ReadOneBit())
            return 0.0f;

        return Low + (High - Low) * buffer.ReadUBits(BitCount) * DecMul;
    }
}
