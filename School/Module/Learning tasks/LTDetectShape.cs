﻿using GoodAI.Modules.School.Common;
using System;
using System.Drawing;

namespace GoodAI.Modules.School.LearningTasks
{
    class LTDetectShape : AbstractLearningTask<ManInWorld>
    {
        protected Random m_rndGen = new Random();
        protected GameObject m_target;
        protected Shape.Shapes m_target_type;

        public LTDetectShape() { }

        public LTDetectShape(ManInWorld w)
            : base(w)
        {
            TSHints = new TrainingSetHints {
                {TSHintAttributes.NOISE, 0},
                {TSHintAttributes.RANDOMNESS, 0},
                {TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 10000}
            };

            TSProgression.Add(TSHints.Clone());
            TSProgression.Add(TSHintAttributes.NOISE, 1);
            TSProgression.Add(TSHintAttributes.RANDOMNESS, 0.5f);
            TSProgression.Add(TSHintAttributes.RANDOMNESS, 1.0f);
            TSProgression.Add(TSHintAttributes.MAX_NUMBER_OF_ATTEMPTS, 100);

            SetHints(TSHints);
        }

        protected override void PresentNewTrainingUnit()
        {
            World.CreateNonVisibleAgent();

            if (TSHints[TSHintAttributes.NOISE] > 0)
            {
                World.IsImageNoise = true;
            }

            // wtih Pr=.5 show object
            if (LearningTaskHelpers.FlipCoin(m_rndGen))
            {
                //random size
                Size shapeSize = new Size(32, 32);
                if (TSHints[TSHintAttributes.RANDOMNESS] >= 0.5)
                {
                    shapeSize = new Size(20 + m_rndGen.Next(20), 20 + m_rndGen.Next(20));
                }

                // random position
                Point shapePosition = World.Agent.GetGeometry().Location + new Size(20, 0);
                if (TSHints[TSHintAttributes.RANDOMNESS] >= 1)
                {
                    shapePosition = World.GetRandomPositionInsidePow(m_rndGen, shapeSize);
                }

                //with Pr=.5 pick Square, else pick Circle
                if (LearningTaskHelpers.FlipCoin(m_rndGen))
                {
                    m_target_type = Shape.Shapes.Circle;
                }
                else
                {
                    m_target_type = Shape.Shapes.Square;
                }

                m_target = World.CreateShape(shapePosition, m_target_type, Color.White, shapeSize);
            }
            else
            {
                m_target = null;
            }
        }

        protected override bool DidTrainingUnitComplete(ref bool wasUnitSuccessful)
        {
            if (m_target == null)
            {
                if (World.Controls.Host[0] != 0 || World.Controls.Host[1] != 0)
                {
                    wasUnitSuccessful = false;

                }
                else
                {
                    wasUnitSuccessful = true;
                }

            }
            else
            {
                if (m_target_type == Shape.Shapes.Circle && World.Controls.Host[0] != 0 && World.Controls.Host[1] <= 0.01f)
                {
                    wasUnitSuccessful = true;
                }
                else if (m_target_type == Shape.Shapes.Square && World.Controls.Host[1] != 0 && World.Controls.Host[0] <= 0.01f)
                {
                    wasUnitSuccessful = true;
                }
                else
                {
                    wasUnitSuccessful = false;
                }
            }
            return true;
        }

    }
}
