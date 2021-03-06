﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Bindings;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects.Drawables.Pieces;
using osuTK;
using osu.Game.Rulesets.Scoring;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public class DrawableHitCircle : DrawableOsuHitObject, IDrawableHitObjectWithProxiedApproach
    {
        public ApproachCircle ApproachCircle;

        private readonly IBindable<Vector2> positionBindable = new Bindable<Vector2>();
        private readonly IBindable<int> stackHeightBindable = new Bindable<int>();
        private readonly IBindable<float> scaleBindable = new Bindable<float>();

        public OsuAction? HitAction => hitArea.HitAction;

        private readonly Container scaleContainer;

        private readonly HitArea hitArea;

        private readonly SkinnableDrawable mainContent;

        public DrawableHitCircle(HitCircle h)
            : base(h)
        {
            Origin = Anchor.Centre;

            Position = HitObject.StackedPosition;

            InternalChildren = new Drawable[]
            {
                scaleContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Children = new Drawable[]
                    {
                        hitArea = new HitArea
                        {
                            Hit = () =>
                            {
                                if (AllJudged)
                                    return false;

                                UpdateResult(true);
                                return true;
                            },
                        },
                        mainContent = new SkinnableDrawable(new OsuSkinComponent(OsuSkinComponents.HitCircle), _ => new MainCirclePiece(HitObject.IndexInCurrentCombo)),
                        ApproachCircle = new ApproachCircle
                        {
                            Alpha = 0,
                            Scale = new Vector2(4),
                        }
                    }
                },
            };

            Size = hitArea.DrawSize;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            positionBindable.BindValueChanged(_ => Position = HitObject.StackedPosition);
            stackHeightBindable.BindValueChanged(_ => Position = HitObject.StackedPosition);
            scaleBindable.BindValueChanged(scale => scaleContainer.Scale = new Vector2(scale.NewValue), true);

            positionBindable.BindTo(HitObject.PositionBindable);
            stackHeightBindable.BindTo(HitObject.StackHeightBindable);
            scaleBindable.BindTo(HitObject.ScaleBindable);

            AccentColour.BindValueChanged(accent => ApproachCircle.Colour = accent.NewValue, true);
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            Debug.Assert(HitObject.HitWindows != null);

            if (!userTriggered)
            {
                if (!HitObject.HitWindows.CanBeHit(timeOffset))
                    ApplyResult(r => r.Type = HitResult.Miss);

                return;
            }

            var result = HitObject.HitWindows.ResultFor(timeOffset);

            if (result == HitResult.None)
            {
                Shake(Math.Abs(timeOffset) - HitObject.HitWindows.HalfWindowFor(HitResult.Miss));
                return;
            }

            ApplyResult(r => r.Type = result);
        }

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();

            mainContent.FadeInFromZero(HitObject.TimeFadeIn);

            ApproachCircle.FadeIn(Math.Min(HitObject.TimeFadeIn * 2, HitObject.TimePreempt));
            ApproachCircle.ScaleTo(1f, HitObject.TimePreempt);
            ApproachCircle.Expire(true);
        }

        protected override void UpdateStateTransforms(ArmedState state)
        {
            Debug.Assert(HitObject.HitWindows != null);

            switch (state)
            {
                case ArmedState.Idle:
                    this.Delay(HitObject.TimePreempt).FadeOut(500);

                    Expire(true);

                    hitArea.HitAction = null;

                    // override lifetime end as FadeIn may have been changed externally, causing out expiration to be too early.
                    LifetimeEnd = HitObject.StartTime + HitObject.HitWindows.HalfWindowFor(HitResult.Miss);
                    break;

                case ArmedState.Miss:
                    ApproachCircle.FadeOut(50);
                    this.FadeOut(100);
                    Expire();
                    break;

                case ArmedState.Hit:
                    ApproachCircle.FadeOut(50);

                    // todo: temporary / arbitrary
                    this.Delay(800).Expire();
                    break;
            }
        }

        public Drawable ProxiedLayer => ApproachCircle;

        private class HitArea : Drawable, IKeyBindingHandler<OsuAction>
        {
            // IsHovered is used
            public override bool HandlePositionalInput => true;

            public Func<bool> Hit;

            public OsuAction? HitAction;

            public HitArea()
            {
                Size = new Vector2(OsuHitObject.OBJECT_RADIUS * 2);

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            public bool OnPressed(OsuAction action)
            {
                switch (action)
                {
                    case OsuAction.LeftButton:
                    case OsuAction.RightButton:
                        if (IsHovered && (Hit?.Invoke() ?? false))
                        {
                            HitAction = action;
                            return true;
                        }

                        break;
                }

                return false;
            }

            public bool OnReleased(OsuAction action) => false;
        }
    }
}
