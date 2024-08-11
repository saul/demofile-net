#pragma warning disable CS1591

namespace DemoFile;

public struct TempEntityEvents
{
    public Action<CMsgTEEffectDispatch>? EffectDispatch;
    public Action<CMsgTEArmorRicochet>? ArmorRicochet;
    public Action<CMsgTEBeamEntPoint>? BeamEntPoint;
    public Action<CMsgTEBeamEnts>? BeamEnts;
    public Action<CMsgTEBeamPoints>? BeamPoints;
    public Action<CMsgTEBeamRing>? BeamRing;
    public Action<CMsgTEBSPDecal>? BSPDecal;
    public Action<CMsgTEBubbles>? Bubbles;
    public Action<CMsgTEBubbleTrail>? BubbleTrail;
    public Action<CMsgTEDecal>? Decal;
    public Action<CMsgTEWorldDecal>? WorldDecal;
    public Action<CMsgTEEnergySplash>? EnergySplash;
    public Action<CMsgTEFizz>? Fizz;
    public Action<CMsgTEShatterSurface>? ShatterSurface;
    public Action<CMsgTEGlowSprite>? GlowSprite;
    public Action<CMsgTEImpact>? Impact;
    public Action<CMsgTEMuzzleFlash>? MuzzleFlash;
    public Action<CMsgTEBloodStream>? BloodStream;
    public Action<CMsgTEExplosion>? Explosion;
    public Action<CMsgTEDust>? Dust;
    public Action<CMsgTELargeFunnel>? LargeFunnel;
    public Action<CMsgTESparks>? Sparks;
    public Action<CMsgTEPhysicsProp>? PhysicsProp;
    public Action<CMsgTEPlayerDecal>? PlayerDecal;
    public Action<CMsgTEProjectedDecal>? ProjectedDecal;
    public Action<CMsgTESmoke>? Smoke;

    internal bool ParseNetMessage(int msgType, ReadOnlySpan<byte> buf)
    {
        switch (msgType)
        {
            case (int) ETEProtobufIds.TeEffectDispatchId:
                EffectDispatch?.Invoke(CMsgTEEffectDispatch.Parser.ParseFrom(buf));
                return true;
            /*
            case (int) ETEProtobufIds.TeArmorRicochetId:
                ArmorRicochet?.Invoke(CMsgTEArmorRicochet.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeBeamEntPointId:
                BeamEntPoint?.Invoke(CMsgTEBeamEntPoint.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeBeamEntsId:
                BeamEnts?.Invoke(CMsgTEBeamEnts.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeBeamPointsId:
                BeamPoints?.Invoke(CMsgTEBeamPoints.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeBeamRingId:
                BeamRing?.Invoke(CMsgTEBeamRing.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeBspdecalId:
                BSPDecal?.Invoke(CMsgTEBSPDecal.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeBubblesId:
                Bubbles?.Invoke(CMsgTEBubbles.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeBubbleTrailId:
                BubbleTrail?.Invoke(CMsgTEBubbleTrail.Parser.ParseFrom(buf));
                return true;
            */
            case (int) ETEProtobufIds.TeDecalId:
                Decal?.Invoke(CMsgTEDecal.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeWorldDecalId:
                WorldDecal?.Invoke(CMsgTEWorldDecal.Parser.ParseFrom(buf));
                return true;
            /*
            case (int) ETEProtobufIds.TeEnergySplashId:
                EnergySplash?.Invoke(CMsgTEEnergySplash.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeFizzId:
                Fizz?.Invoke(CMsgTEFizz.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeShatterSurfaceId:
                ShatterSurface?.Invoke(CMsgTEShatterSurface.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeGlowSpriteId:
                GlowSprite?.Invoke(CMsgTEGlowSprite.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeImpactId:
                Impact?.Invoke(CMsgTEImpact.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeMuzzleFlashId:
                MuzzleFlash?.Invoke(CMsgTEMuzzleFlash.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeBloodStreamId:
                BloodStream?.Invoke(CMsgTEBloodStream.Parser.ParseFrom(buf));
                return true;
            */
            case (int) ETEProtobufIds.TeExplosionId:
                Explosion?.Invoke(CMsgTEExplosion.Parser.ParseFrom(buf));
                return true;
            /*
            case (int) ETEProtobufIds.TeDustId:
                Dust?.Invoke(CMsgTEDust.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeLargeFunnelId:
                LargeFunnel?.Invoke(CMsgTELargeFunnel.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeSparksId:
                Sparks?.Invoke(CMsgTESparks.Parser.ParseFrom(buf));
                return true;
            */
            case (int) ETEProtobufIds.TePhysicsPropId:
                PhysicsProp?.Invoke(CMsgTEPhysicsProp.Parser.ParseFrom(buf));
                return true;
            /*
            case (int) ETEProtobufIds.TePlayerDecalId:
                PlayerDecal?.Invoke(CMsgTEPlayerDecal.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeProjectedDecalId:
                ProjectedDecal?.Invoke(CMsgTEProjectedDecal.Parser.ParseFrom(buf));
                return true;
            case (int) ETEProtobufIds.TeSmokeId:
                Smoke?.Invoke(CMsgTESmoke.Parser.ParseFrom(buf));
                return true;
            */
        }

        return false;
    }
}