﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu/master/LICENCE

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Game.Screens.Edit.Screens.Compose
{
    public class BeatDivisorControl : CompositeDrawable
    {
        private readonly BindableBeatDivisor beatDivisor = new BindableBeatDivisor();
        private int currentDivisorIndex;
        private TickSliderBar slider;

        public BeatDivisorControl(BindableBeatDivisor beatDivisor)
        {
            this.beatDivisor.BindTo(beatDivisor);
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            Masking = true;
            CornerRadius = 5;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    Name = "Background",
                    RelativeSizeAxes = Axes.Both,
                    Colour = Color4.Black
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            slider = new TickSliderBar(beatDivisor, 1, 2, 3, 4, 6, 8, 12, 16)
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Horizontal = 5 }
                            }
                        },
                        new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colours.Gray4
                                    },
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding { Horizontal = 5 },
                                        Child = new GridContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Content = new[]
                                            {
                                                new Drawable[]
                                                {
                                                    new DivisorButton
                                                    {
                                                        Icon = FontAwesome.fa_chevron_left,
                                                        Action = beatDivisor.Previous
                                                    },
                                                    new DivisorText(beatDivisor),
                                                    new DivisorButton
                                                    {
                                                        Icon = FontAwesome.fa_chevron_right,
                                                        Action = beatDivisor.Next
                                                    }
                                                },
                                                new Drawable[]
                                                {
                                                    null,
                                                    new TextFlowContainer(s => s.TextSize = 10)
                                                    {
                                                        Text = "beat snap divisor",
                                                        RelativeSizeAxes = Axes.X,
                                                        TextAnchor = Anchor.TopCentre
                                                    },
                                                },
                                            },
                                            ColumnDimensions = new[]
                                            {
                                                new Dimension(GridSizeMode.Absolute, 20),
                                                new Dimension(),
                                                new Dimension(GridSizeMode.Absolute, 20)
                                            }
                                        }
                                    }
                                }
                            }
                        },
                    },
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 35),
                    }
                }
            };

            slider.Current.BindTo(beatDivisor);
        }

        private class DivisorText : SpriteText
        {
            private readonly Bindable<int> beatDivisor = new Bindable<int>();

            public DivisorText(BindableBeatDivisor beatDivisor)
            {
                this.beatDivisor.BindTo(beatDivisor);

                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                Colour = colours.BlueLighter;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                beatDivisor.ValueChanged += v => updateText();
                updateText();
            }

            private void updateText() => Text = $"1/{beatDivisor.Value}";
        }

        private class DivisorButton : IconButton
        {
            public DivisorButton()
            {
                Anchor = Anchor.Centre;
                Origin = Anchor.Centre;

                // Small offset to look a bit better centered along with the divisor text
                Y = 1;

                ButtonSize = new Vector2(20);
                IconScale = new Vector2(0.6f);
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                IconColour = Color4.Black;
                HoverColour = colours.Gray7;
                FlashColour = colours.Gray9;
            }
        }

        private class TickSliderBar : SliderBar<int>
        {
            public new MarginPadding Padding
            {
                set => base.Padding = value;
            }

            private Marker marker;

            private readonly int[] availableDivisors;

            public TickSliderBar(params int[] divisors)
            {
                availableDivisors = divisors;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                foreach (var t in availableDivisors)
                {
                    AddInternal(new Tick(t)
                    {
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.TopCentre,
                        RelativePositionAxes = Axes.X,
                        X = getTickPosition(t)
                    });
                }

                AddInternal(marker = new Marker());

                CurrentNumber.ValueChanged += v =>
                {
                    marker.MoveToX(getTickPosition(v), 100, Easing.OutQuint);
                    marker.Flash();

                    KeyboardStep = v / 3f;
                };
            }

            protected override void UpdateValue(float value)
            {
            }

            protected override bool OnMouseDown(InputState state, MouseDownEventArgs args)
            {
                marker.Active = true;
                return base.OnMouseDown(state, args);
            }

            protected override bool OnMouseUp(InputState state, MouseUpEventArgs args)
            {
                marker.Active = false;
                return base.OnMouseUp(state, args);
            }

            protected override bool OnClick(InputState state)
            {
                handleMouseInput(state);
                return true;
            }

            protected override bool OnDrag(InputState state)
            {
                handleMouseInput(state);
                return true;
            }

            private void handleMouseInput(InputState state)
            {
                // copied from SliderBar so we can do custom spacing logic.
                var xPosition = ToLocalSpace(state?.Mouse.NativeState.Position ?? Vector2.Zero).X - RangePadding;

                if (!CurrentNumber.Disabled)
                    CurrentNumber.SetProportional(xPosition / UsableWidth, state != null && state.Keyboard.ShiftPressed ? KeyboardStep : 0);

                OnUserChange();
            }

            private float getTickPosition(float divisor) => (divisor - 1) / availableDivisors.Last();

            private class Tick : CompositeDrawable
            {
                private readonly int divisor;

                public Tick(int divisor)
                {
                    this.divisor = divisor;
                    Size = new Vector2(2.5f, 10);

                    InternalChild = new Box { RelativeSizeAxes = Axes.Both };

                    CornerRadius = 0.5f;
                    Masking = true;
                }

                [BackgroundDependencyLoader]
                private void load(OsuColour colours)
                {
                    if (divisor >= 16)
                        Colour = colours.Red;
                    else if (divisor >= 8)
                        Colour = colours.Yellow;
                    else
                        Colour = OsuColour.Gray(2f / divisor);
                }
            }

            private class Marker : CompositeDrawable
            {
                private Color4 defaultColour;

                private const float size = 7;

                [BackgroundDependencyLoader]
                private void load(OsuColour colours)
                {
                    Colour = defaultColour = colours.Gray4;
                    Anchor = Anchor.TopLeft;
                    Origin = Anchor.TopCentre;

                    Width = size;
                    RelativeSizeAxes = Axes.Y;
                    RelativePositionAxes = Axes.X;

                    InternalChildren = new Drawable[]
                    {
                        new Box
                        {
                            Width = 2,
                            RelativeSizeAxes = Axes.Y,
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Colour = ColourInfo.GradientVertical(Color4.Transparent, Color4.White),
                            Blending = BlendingMode.Additive,
                        },
                        new EquilateralTriangle
                        {
                            Origin = Anchor.BottomCentre,
                            Anchor = Anchor.BottomCentre,
                            Height = size,
                            EdgeSmoothness = new Vector2(1),
                            Colour = Color4.White,
                        }
                    };
                }

                private bool active;

                public bool Active
                {
                    get => active;
                    set
                    {
                        this.FadeColour(value ? Color4.White : defaultColour, 500, Easing.OutQuint);
                        active = value;
                    }
                }

                public void Flash()
                {
                    bool wasActive = active;

                    Active = true;

                    if (wasActive) return;

                    using (BeginDelayedSequence(50))
                        Active = false;
                }
            }
        }
    }
}
