namespace DemoFile.Test;

[TestFixture]
public class QuantizedFloatEncodingTest
{
    [Test]
    public void Create()
    {
        QuantizedFloatEncoding qf;
        QuantizedFloatEncoding expected;
            
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 15, 1, null, 1024));
        expected = new QuantizedFloatEncoding(
            0.0f,
            1023.96875000000000000000000000000000f,
            0.00003051850944757461547851562500f,
            15,
            0);

        Assert.That(qf, Is.EqualTo(expected));

        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 15, 1, default, 1024.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 1023.96875000000000000000000000000000f,
            //high_low_mul: 32.00000000000000000000000000000000f,
            DecMul: 0.00003051850944757461547851562500f,
            //offset: 0.03125000000000000000000000000000f,
            BitCount: 15,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, 5, -4.000000f, 12.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -4.00000000000000000000000000000000f,
            High: 11.93750000000000000000000000000000f,
            //high_low_mul: 16.00000000000000000000000000000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.06250000000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, default, default, 1.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 1.00000000000000000000000000000000f,
            //high_low_mul: 255.00000000000000000000000000000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));

        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 10, 1, default, 1024.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 1023.00000000000000000000000000000000f,
            //high_low_mul: 1.00000000000000000000000000000000f,
            DecMul: 0.00097751710563898086547851562500f,
            //offset: 1.00000000000000000000000000000000f,
            BitCount: 10,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 10, 1, default, 256.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 255.75000000000000000000000000000000f,
            //high_low_mul: 4.00000000000000000000000000000000f,
            DecMul: 0.00097751710563898086547851562500f,
            //offset: 0.25000000000000000000000000000000f,
            BitCount: 10,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 18, 4, -4096.000000f, 4096.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -4096.00000000000000000000000000000000f,
            High: 4096.00000000000000000000000000000000f,
            //high_low_mul: 31.99987792968750000000000000000000f,
            DecMul: 0.00000381471181754022836685180664f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 18,
            Flags: (QuantizedFloatFlags)4
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 18, 4, -4096.000000f, 4096.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -4096.00000000000000000000000000000000f,
            High: 4096.00000000000000000000000000000000f,
            //high_low_mul: 31.99987792968750000000000000000000f,
            DecMul: 0.00000381471181754022836685180664f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 18,
            Flags: (QuantizedFloatFlags)4
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 18, 4, -4096.000000f, 4096.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -4096.00000000000000000000000000000000f,
            High: 4096.00000000000000000000000000000000f,
            //high_low_mul: 31.99987792968750000000000000000000f,
            DecMul: 0.00000381471181754022836685180664f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 18,
            Flags: (QuantizedFloatFlags)4
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 15, 1, default, 1024.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 1023.96875000000000000000000000000000f,
            //high_low_mul: 32.00000000000000000000000000000000f,
            DecMul: 0.00003051850944757461547851562500f,
            //offset: 0.03125000000000000000000000000000f,
            BitCount: 15,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 15, 1, default, 1024.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 1023.96875000000000000000000000000000f,
            //high_low_mul: 32.00000000000000000000000000000000f,
            DecMul: 0.00003051850944757461547851562500f,
            //offset: 0.03125000000000000000000000000000f,
            BitCount: 15,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 15, 1, default, 1024.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 1023.96875000000000000000000000000000f,
            //high_low_mul: 32.00000000000000000000000000000000f,
            DecMul: 0.00003051850944757461547851562500f,
            //offset: 0.03125000000000000000000000000000f,
            BitCount: 15,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, 1, default, 4.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 3.98437500000000000000000000000000f,
            //high_low_mul: 64.00000000000000000000000000000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.01562500000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 32, default, default, default));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 0.00000000000000000000000000000000f,
            //high_low_mul: 0.00000000000000000000000000000000f,
            DecMul: 0.00000000000000000000000000000000f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 32,
            Flags: (QuantizedFloatFlags)0
            //no_scale: true,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 32, default, default, default));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 0.00000000000000000000000000000000f,
            //high_low_mul: 0.00000000000000000000000000000000f,
            DecMul: 0.00000000000000000000000000000000f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 32,
            Flags: (QuantizedFloatFlags)0
            //no_scale: true,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 32, default, default, default));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 0.00000000000000000000000000000000f,
            //high_low_mul: 0.00000000000000000000000000000000f,
            DecMul: 0.00000000000000000000000000000000f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 32,
            Flags: (QuantizedFloatFlags)0
            //no_scale: true,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 10, 4, -64.000000f, 64.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -64.00000000000000000000000000000000f,
            High: 64.00000000000000000000000000000000f,
            //high_low_mul: 7.99218750000000000000000000000000f,
            DecMul: 0.00097751710563898086547851562500f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 10,
            Flags: (QuantizedFloatFlags)4
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 10, 4, -64.000000f, 64.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -64.00000000000000000000000000000000f,
            High: 64.00000000000000000000000000000000f,
            //high_low_mul: 7.99218750000000000000000000000000f,
            DecMul: 0.00097751710563898086547851562500f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 10,
            Flags: (QuantizedFloatFlags)4
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 20, 4, default, 128.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 127.99987792968750000000000000000000f,
            //high_low_mul: 8192.00000000000000000000000000000000f,
            DecMul: 0.00000095367522590095177292823792f,
            //offset: 0.00012207031250000000000000000000f,
            BitCount: 20,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 20, 1, default, 256.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 255.99975585937500000000000000000000f,
            //high_low_mul: 4096.00000000000000000000000000000000f,
            DecMul: 0.00000095367522590095177292823792f,
            //offset: 0.00024414062500000000000000000000f,
            BitCount: 20,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 10, 2, -25.000000f, 25.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -24.95117187500000000000000000000000f,
            High: 25.00000000000000000000000000000000f,
            //high_low_mul: 20.47999954223632812500000000000000f,
            DecMul: 0.00097751710563898086547851562500f,
            //offset: 0.04882812500000000000000000000000f,
            BitCount: 10,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 10, 2, default, 102.300003f));
        expected = new QuantizedFloatEncoding(
            Low: 0.09990234673023223876953125000000f,
            High: 102.30000305175781250000000000000000f,
            //high_low_mul: 10.00977420806884765625000000000000f,
            DecMul: 0.00097751710563898086547851562500f,
            //offset: 0.09990234673023223876953125000000f,
            BitCount: 10,
            Flags: (QuantizedFloatFlags)2
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 10, 2, default, 102.300003f));
        expected = new QuantizedFloatEncoding(
            Low: 0.09990234673023223876953125000000f,
            High: 102.30000305175781250000000000000000f,
            //high_low_mul: 10.00977420806884765625000000000000f,
            DecMul: 0.00097751710563898086547851562500f,
            //offset: 0.09990234673023223876953125000000f,
            BitCount: 10,
            Flags: (QuantizedFloatFlags)2
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, 1, default, 64.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 63.75000000000000000000000000000000f,
            //high_low_mul: 4.00000000000000000000000000000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.25000000000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, 1, default, 256.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 255.00000000000000000000000000000000f,
            //high_low_mul: 1.00000000000000000000000000000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 1.00000000000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, default, default, 100.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 100.00000000000000000000000000000000f,
            //high_low_mul: 2.54999995231628417968750000000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 12, 1, default, 2048.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 2047.50000000000000000000000000000000f,
            //high_low_mul: 2.00000000000000000000000000000000f,
            DecMul: 0.00024420025874860584735870361328f,
            //offset: 0.50000000000000000000000000000000f,
            BitCount: 12,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 17, 4, -4096.000000f, 4096.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -4096.00000000000000000000000000000000f,
            High: 4096.00000000000000000000000000000000f,
            //high_low_mul: 15.99987792968750000000000000000000f,
            DecMul: 0.00000762945273891091346740722656f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 17,
            Flags: (QuantizedFloatFlags)4
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, default, default, 360.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 360.00000000000000000000000000000000f,
            //high_low_mul: 0.70833331346511840820312500000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, default, default, 360.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 360.00000000000000000000000000000000f,
            //high_low_mul: 0.70833331346511840820312500000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 16, 1, default, 500.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 499.99237060546875000000000000000000f,
            //high_low_mul: 131.05889892578125000000000000000000f,
            DecMul: 0.00001525902189314365386962890625f,
            //offset: 0.00762939453125000000000000000000f,
            BitCount: 16,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 18, 1, default, 1500.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 1499.99426269531250000000000000000000f,
            //high_low_mul: 174.76266479492187500000000000000000f,
            DecMul: 0.00000381471181754022836685180664f,
            //offset: 0.00572204589843750000000000000000f,
            BitCount: 18,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 11, default, -1.000000f, 63.000000f));
        expected = new QuantizedFloatEncoding(
            Low: -1.00000000000000000000000000000000f,
            High: 63.00000000000000000000000000000000f,
            //high_low_mul: 31.98437500000000000000000000000000f,
            DecMul: 0.00048851978499442338943481445312f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 11,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 7, 1, default, 360.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 357.18750000000000000000000000000000f,
            //high_low_mul: 0.35555556416511535644531250000000f,
            DecMul: 0.00787401571869850158691406250000f,
            //offset: 2.81250000000000000000000000000000f,
            BitCount: 7,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 6, 2, default, 64.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 1.00000000000000000000000000000000f,
            High: 64.00000000000000000000000000000000f,
            //high_low_mul: 1.00000000000000000000000000000000f,
            DecMul: 0.01587301678955554962158203125000f,
            //offset: 1.00000000000000000000000000000000f,
            BitCount: 6,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, 1, default, 1.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.00000000000000000000000000000000f,
            High: 0.99609375000000000000000000000000f,
            //high_low_mul: 256.00000000000000000000000000000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.00390625000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 10, default, 0.100000f, 10.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.10000000149011611938476562500000f,
            High: 10.00000000000000000000000000000000f,
            //high_low_mul: 103.33333587646484375000000000000000f,
            DecMul: 0.00097751710563898086547851562500f,
            //offset: 0.00000000000000000000000000000000f,
            BitCount: 10,
            Flags: (QuantizedFloatFlags)0
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
        qf = QuantizedFloatEncoding.Create(new FieldEncodingInfo(null, 8, 2, default, 60.000000f));
        expected = new QuantizedFloatEncoding(
            Low: 0.23437500000000000000000000000000f,
            High: 60.00000000000000000000000000000000f,
            //high_low_mul: 4.26624011993408203125000000000000f,
            DecMul: 0.00392156885936856269836425781250f,
            //offset: 0.23437500000000000000000000000000f,
            BitCount: 8,
            Flags: (QuantizedFloatFlags)2
            //no_scale: false,
        );
        Assert.That(qf, Is.EqualTo(expected));
    }
}
