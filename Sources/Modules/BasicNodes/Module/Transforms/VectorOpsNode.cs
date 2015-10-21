﻿using GoodAI.BasicNodes.Transforms;
using GoodAI.Core.Memory;
using GoodAI.Core.Nodes;
using GoodAI.Core.Task;
using GoodAI.Core.Utils;
using GoodAI.Modules.Matrix;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YAXLib;

namespace GoodAI.Modules.Transforms
{
    /// <author>GoodAI</author>
    /// <meta>mv</meta>
    /// <status>Working</status>
    /// <summary>Performs 2D vectors operations. Supports rotation and computing an angle between vectors</summary>
    /// <description>
    /// </description>
    public class MyVectorOpsNode : MyWorkingNode
    {
        [MyInputBlock]
        public MyMemoryBlock<float> InputA
        {
            get { return GetInput(0); }
        }

        [MyInputBlock]
        public MyMemoryBlock<float> InputB
        {
            get { return GetInput(1); }
        }

        [MyOutputBlock]
        public MyMemoryBlock<float> Output
        {
            get { return GetOutput(0); }
            set { SetOutput(0, value); }
        }

        private MyMemoryBlock<float> Temp { get; set; }

        [MyTaskGroup("VectorOp")]
        public MyRotateTask RotateTask { get; private set; }
        [MyTaskGroup("VectorOp")]
        public MyAngleTask AngleTask { get; private set; }

        public override void UpdateMemoryBlocks()
        {
            Output.Count = InputA != null ? InputA.Count : 1;
            Output.ColumnHint = InputA != null ? InputA.ColumnHint : 1;

            Temp.ColumnHint = Output.Count;
            Temp.Count = Temp.ColumnHint * Temp.ColumnHint;
        }

        /// <summary>
        /// Rotates 2D vector in first input by number of degrees specified in the first element of the second input
        /// </summary>
        [Description("Rotates 2D vector")]
        public class MyRotateTask : MyTask<MyVectorOpsNode>
        {
            private VectorOps m_vecOps;

            public override void Init(int nGPU)
            {
                m_vecOps = new VectorOps(Owner, VectorOps.VectorOperation.Rotate, Owner.Temp);
            }

            public override void Execute()
            {
                m_vecOps.Run(VectorOps.VectorOperation.Rotate, Owner.InputA, Owner.InputB, Owner.Output);
            }
        }

        /// <summary>
        /// Computes (un)directed angle between two 2D vectors
        /// </summary>
        [Description("Angle between 2 vectors")]
        public class MyAngleTask : MyTask<MyVectorOpsNode>
        {
            [MyBrowsable, Category("Params"), Description("Computes directed angle if set to True")]
            [YAXSerializableField(DefaultValue = false)]
            public bool Directed { get; set; }

            private VectorOps m_vecOps;

            public override void Init(int nGPU)
            {
                m_vecOps = new VectorOps(Owner, VectorOps.VectorOperation.Angle | VectorOps.VectorOperation.DirectedAngle, Owner.Temp);
            }

            public override void Execute()
            {
                if (Directed)
                {
                    m_vecOps.Run(VectorOps.VectorOperation.DirectedAngle, Owner.InputA, Owner.InputB, Owner.Output);
                }
                else
                {
                    m_vecOps.Run(VectorOps.VectorOperation.Angle, Owner.InputA, Owner.InputB, Owner.Output);
                }
            }
        }
    }
}