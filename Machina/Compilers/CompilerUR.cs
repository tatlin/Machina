﻿using System;
using System.Collections.Generic;

namespace Machina
{
    //   ██████╗ ██████╗ ███╗   ███╗██████╗ ██╗██╗     ███████╗██████╗ 
    //  ██╔════╝██╔═══██╗████╗ ████║██╔══██╗██║██║     ██╔════╝██╔══██╗
    //  ██║     ██║   ██║██╔████╔██║██████╔╝██║██║     █████╗  ██████╔╝
    //  ██║     ██║   ██║██║╚██╔╝██║██╔═══╝ ██║██║     ██╔══╝  ██╔══██╗
    //  ╚██████╗╚██████╔╝██║ ╚═╝ ██║██║     ██║███████╗███████╗██║  ██║
    //   ╚═════╝ ╚═════╝ ╚═╝     ╚═╝╚═╝     ╚═╝╚══════╝╚══════╝╚═╝  ╚═╝

    //  ██╗   ██╗██████╗ 
    //  ██║   ██║██╔══██╗
    //  ██║   ██║██████╔╝
    //  ██║   ██║██╔══██╗
    //  ╚██████╔╝██║  ██║
    //   ╚═════╝ ╚═╝  ╚═╝

    /// <summary>
    /// A compiler for Universal Robots 6-axis robotic arms.
    /// </summary>
    internal class CompilerUR : Compiler
    {
        // From the URScript manual
        public static readonly char COMMENT_CHAR = '#';
        public static readonly double DEFAULT_JOINT_ACCELERATION = 1.4;
        public static readonly double DEFAULT_JOINT_SPEED = 1.05;
        public static readonly double DEFAULT_TOOL_ACCELERATION = 1.2;
        public static readonly double DEFAULT_TOOL_SPEED = 0.25;
        
        internal CompilerUR() : base("#") { }

        /// <summary>
        /// Creates a textual program representation of a set of Actions using native UR Script.
        /// </summary>
        /// <param name="programName"></param>
        /// <param name="writePointer"></param>
        /// <param name="block">Use actions in waiting queue or buffer?</param>
        /// <returns></returns>
        public override List<string> UNSAFEProgramFromBuffer(string programName, RobotCursor writer, bool block, bool inlineTargets, bool humanComments)
        {
            // @TODO: deprecate all instantiation shit, and make compilers be mostly static 
            //ADD_ACTION_STRING = humanComments;

            // Which pending Actions are used for this program?
            // Copy them without flushing the buffer.
            List<Action> actions = block ?
                writer.actionBuffer.GetBlockPending(false) :
                writer.actionBuffer.GetAllPending(false);


            // CODE LINES GENERATION
            // TARGETS AND INSTRUCTIONS
            List<string> variableLines = new List<string>();
            List<string> instructionLines = new List<string>();

            // DATA GENERATION
            // Use the write RobotCursor to generate the data
            int it = 0;
            string line = null;
            foreach (Action a in actions)
            {
                // Move writerCursor to this action state
                writer.ApplyNextAction();  // for the buffer to correctly manage them 

                if (inlineTargets)
                {
                    if (GenerateInstructionDeclaration(a, writer, humanComments, out line))  // there will be a number jump on target-less instructions, but oh well...
                    {
                        instructionLines.Add(line);
                    }
                }
                else
                {
                    // Generate lines of code
                    if (GenerateVariableDeclaration(a, writer, it, out line))  // there will be a number jump on target-less instructions, but oh well...
                    {
                        variableLines.Add(line);
                    }

                    if (GenerateInstructionDeclarationFromVariable(a, writer, it, humanComments, out line))  // there will be a number jump on target-less instructions, but oh well...
                    {
                        instructionLines.Add(line);
                    }
                }

                // Move on
                it++;
            }


            // PROGRAM ASSEMBLY
            // Initialize a module list
            List<string> module = new List<string>();

            // Banner
            module.AddRange(GenerateDisclaimerHeader(programName));
            module.Add("");

            // MODULE HEADER
            module.Add("def " + programName + "():");
            module.Add("");

            // Targets
            if (variableLines.Count != 0)
            {
                module.AddRange(variableLines);
                module.Add("");
            }

            // MAIN PROCEDURE
            // Instructions
            if (instructionLines.Count != 0)
            {
                module.AddRange(instructionLines);
                module.Add("");
            }

            module.Add("end");
            module.Add("");

            // MODULE KICKOFF
            module.Add(programName + "()");

            return module;
        }




        //  ╦ ╦╔╦╗╦╦  ╔═╗
        //  ║ ║ ║ ║║  ╚═╗
        //  ╚═╝ ╩ ╩╩═╝╚═╝
        internal static bool GenerateVariableDeclaration(Action action, RobotCursor cursor, int id, out string declaration)
        {
            string dec = null;
            switch (action.type)
            {
                case ActionType.Translation:
                case ActionType.Rotation:
                case ActionType.Transformation:
                    dec = string.Format("  target{0}={1}", id, GetPoseTargetValue(cursor));
                    break;

                case ActionType.Axes:
                    dec = string.Format("  target{0}={1}", id, GetJointTargetValue(cursor));
                    break;
            }

            declaration = dec;
            return dec != null;
        }

        internal static bool GenerateInstructionDeclarationFromVariable(
            Action action, RobotCursor cursor, int id, bool humanComments,
            out string declaration)
        {
            string dec = null;
            switch (action.type)
            {
                case ActionType.Translation:
                case ActionType.Rotation:
                case ActionType.Transformation:
                    // Accelerations and velocoties have different meaning for moveJ and moveL instructions.
                    // Joint motion is essentially the same as Axes motion, just the input is a pose instead of a joints vector.
                    if (cursor.motionType == MotionType.Joint)
                    {
                        dec = string.Format("  movej(target{0}, a={1}, v={2}, r={3})",
                            id,
                            cursor.jointAcceleration > Geometry.EPSILON2 ? Math.Round(Geometry.TO_RADS * cursor.jointAcceleration, Geometry.STRING_ROUND_DECIMALS_RADS) : DEFAULT_JOINT_ACCELERATION,
                            cursor.jointSpeed > Geometry.EPSILON2 ? Math.Round(Geometry.TO_RADS * cursor.jointSpeed, Geometry.STRING_ROUND_DECIMALS_RADS) : DEFAULT_JOINT_SPEED,
                            Math.Round(0.001 * cursor.precision, Geometry.STRING_ROUND_DECIMALS_M));
                    }
                    else
                    {
                        dec = string.Format("  movel(target{0}, a={1}, v={2}, r={3})",
                            id,
                            cursor.acceleration > Geometry.EPSILON2 ? Math.Round(0.001 * cursor.acceleration, Geometry.STRING_ROUND_DECIMALS_M) : DEFAULT_TOOL_ACCELERATION,
                            cursor.speed > Geometry.EPSILON2 ? Math.Round(0.001 * cursor.speed, Geometry.STRING_ROUND_DECIMALS_M) : DEFAULT_TOOL_SPEED,
                            Math.Round(0.001 * cursor.precision, Geometry.STRING_ROUND_DECIMALS_M));
                    }
                    break;

                case ActionType.RotationSpeed:
                    dec = string.Format("  {0} WARNING: RotationSpeed() has no effect in UR robots, try JointSpeed() or JointAcceleration() instead", COMMENT_CHAR);
                    break;

                case ActionType.Axes:
                    // HAL generates a "set_tcp(p[0,0,0,0,0,0])" call here which I find confusing... 
                    dec = string.Format("  movej(target{0}, a={1}, v={2}, r={3})",
                        id,
                        cursor.jointAcceleration > Geometry.EPSILON2 ? Math.Round(Geometry.TO_RADS * cursor.jointAcceleration, Geometry.STRING_ROUND_DECIMALS_RADS) : DEFAULT_JOINT_ACCELERATION,
                        cursor.jointSpeed > Geometry.EPSILON2 ? Math.Round(Geometry.TO_RADS * cursor.jointSpeed, Geometry.STRING_ROUND_DECIMALS_RADS) : DEFAULT_JOINT_SPEED,
                        Math.Round(0.001 * cursor.precision, Geometry.STRING_ROUND_DECIMALS_M));
                    break;

                case ActionType.Message:
                    ActionMessage am = (ActionMessage)action;
                    dec = string.Format("  popup(\"{0}\", title=\"Machina Message\", warning=False, error=False)",
                        am.message);
                    break;

                case ActionType.Wait:
                    ActionWait aw = (ActionWait)action;
                    dec = string.Format("  sleep({0})",
                        0.001 * aw.millis);
                    break;

                case ActionType.Comment:
                    ActionComment ac = (ActionComment)action;
                    dec = string.Format("  {0} {1}",
                        COMMENT_CHAR,
                        ac.comment);
                    break;

                case ActionType.Attach:
                    ActionAttach aa = (ActionAttach)action;
                    dec = string.Format("  set_tcp({0})",  // @TODO: should need to add a "set_payload(m, CoG)" dec afterwards...
                        GetToolValue(cursor));
                    break;

                case ActionType.Detach:
                    ActionDetach ad = (ActionDetach)action;
                    dec = string.Format("  set_tcp(p[0,0,0,0,0,0])");  // @TODO: should need to add a "set_payload(m, CoG)" dec afterwards...
                    break;

                case ActionType.IODigital:
                    ActionIODigital aiod = (ActionIODigital)action;
                    if (aiod.pin < 0 || aiod.pin >= cursor.digitalOutputs.Length)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": IO number not available",
                            COMMENT_CHAR,
                            aiod.ToString());
                    }
                    else if (aiod.pin > 7)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": digital IO pin not available in UR robot",
                            COMMENT_CHAR,
                            aiod.ToString());
                    }
                    else
                    {
                        dec = string.Format("  set_standard_digital_out({0}, {1})",
                            aiod.pin,
                            aiod.on ? "True" : "False");
                    }
                    break;

                case ActionType.IOAnalog:
                    ActionIOAnalog aioa = (ActionIOAnalog)action;
                    if (aioa.pin < 0 || aioa.pin >= cursor.analogOutputs.Length)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": IO number not available",
                            COMMENT_CHAR,
                            aioa.ToString());
                    }
                    else if (aioa.pin > 1)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": analog IO pin not available in UR robot",
                            COMMENT_CHAR,
                            aioa.ToString());
                    }
                    else if (aioa.value < 0 || aioa.value > 1)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": value out of range [0.0, 1.0]",
                            COMMENT_CHAR,
                            aioa.ToString());
                    }
                    else
                    {
                        dec = string.Format("  set_standard_analog_out({0}, {1})",
                            aioa.pin,
                            Math.Round(aioa.value, Geometry.STRING_ROUND_DECIMALS_VOLTAGE));
                    }
                    break;

                    //default:
                    //    dec = string.Format("  # ACTION \"{0}\" NOT IMPLEMENTED", action);
                    //    break;
            }

            if (humanComments && action.type != ActionType.Comment)
            {
                dec = string.Format("{0}  {1} [{2}]",
                    dec,
                    COMMENT_CHAR,
                    action.ToString());
            }
            //else if (ADD_ACTION_ID)
            //{
            //    dec = string.Format("{0}  {1} [{2}]",
            //        dec,
            //        commChar,
            //        action.id);
            //}

            declaration = dec;
            return dec != null;
        }

        internal static bool GenerateInstructionDeclaration(
            Action action, RobotCursor cursor, bool humanComments,
            out string declaration)
        {
            string dec = null;
            switch (action.type)
            {
                case ActionType.Translation:
                case ActionType.Rotation:
                case ActionType.Transformation:
                    // Accelerations and velocoties have different meaning for moveJ and moveL instructions.
                    // Joint motion is essentially the same as Axes motion, just the input is a pose instead of a joints vector.
                    if (cursor.motionType == MotionType.Joint)
                    {
                        dec = string.Format("  movej({0}, a={1}, v={2}, r={3})",
                            GetPoseTargetValue(cursor),
                            cursor.jointAcceleration > Geometry.EPSILON2 ? Math.Round(Geometry.TO_RADS * cursor.jointAcceleration, Geometry.STRING_ROUND_DECIMALS_RADS) : DEFAULT_JOINT_ACCELERATION,
                            cursor.jointSpeed > Geometry.EPSILON2 ? Math.Round(Geometry.TO_RADS * cursor.jointSpeed, Geometry.STRING_ROUND_DECIMALS_RADS) : DEFAULT_JOINT_SPEED,
                            Math.Round(0.001 * cursor.precision, Geometry.STRING_ROUND_DECIMALS_M));
                    }
                    else
                    {
                        dec = string.Format("  movel({0}, a={1}, v={2}, r={3})",
                            GetPoseTargetValue(cursor),
                            cursor.acceleration > Geometry.EPSILON2 ? Math.Round(0.001 * cursor.acceleration, Geometry.STRING_ROUND_DECIMALS_M) : DEFAULT_TOOL_ACCELERATION,
                            cursor.speed > Geometry.EPSILON2 ? Math.Round(0.001 * cursor.speed, Geometry.STRING_ROUND_DECIMALS_M) : DEFAULT_TOOL_SPEED,
                            Math.Round(0.001 * cursor.precision, Geometry.STRING_ROUND_DECIMALS_M));
                    }
                    break;

                case ActionType.RotationSpeed:
                    dec = string.Format("  {0} WARNING: RotationSpeed() has no effect in UR robots, try JointSpeed() or JointAcceleration() instead", COMMENT_CHAR);
                    break;

                case ActionType.Axes:
                    // HAL generates a "set_tcp(p[0,0,0,0,0,0])" call here which I find confusing... 
                    dec = string.Format("  movej({0}, a={1}, v={2}, r={3})",
                        GetJointTargetValue(cursor),
                        cursor.jointAcceleration > Geometry.EPSILON2 ? Math.Round(Geometry.TO_RADS * cursor.jointAcceleration, Geometry.STRING_ROUND_DECIMALS_RADS) : DEFAULT_JOINT_ACCELERATION,
                        cursor.jointSpeed > Geometry.EPSILON2 ? Math.Round(Geometry.TO_RADS * cursor.jointSpeed, Geometry.STRING_ROUND_DECIMALS_RADS) : DEFAULT_JOINT_SPEED,
                        Math.Round(0.001 * cursor.precision, Geometry.STRING_ROUND_DECIMALS_M));
                    break;

                case ActionType.Message:
                    ActionMessage am = (ActionMessage)action;
                    dec = string.Format("  popup(\"{0}\", title=\"Machina Message\", warning=False, error=False)",
                        am.message);
                    break;

                case ActionType.Wait:
                    ActionWait aw = (ActionWait)action;
                    dec = string.Format("  sleep({0})",
                        0.001 * aw.millis);
                    break;

                case ActionType.Comment:
                    ActionComment ac = (ActionComment)action;
                    dec = string.Format("  {0} {1}",
                        COMMENT_CHAR,
                        ac.comment);
                    break;

                case ActionType.Attach:
                    ActionAttach aa = (ActionAttach)action;
                    dec = string.Format("  set_tcp({0})",   // @TODO: should need to add a "set_payload(m, CoG)" dec afterwards...
                        GetToolValue(cursor));
                    break;

                case ActionType.Detach:
                    ActionDetach ad = (ActionDetach)action;
                    dec = string.Format("  set_tcp(p[0,0,0,0,0,0])");   // @TODO: should need to add a "set_payload(m, CoG)" dec afterwards...
                    break;

                case ActionType.IODigital:
                    ActionIODigital aiod = (ActionIODigital)action;
                    if (aiod.pin < 0 || aiod.pin >= cursor.digitalOutputs.Length)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": IO number not available",
                            COMMENT_CHAR,
                            aiod.ToString());
                    }
                    else if (aiod.pin > 7)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": digital IO pin not available in UR robot",
                            COMMENT_CHAR,
                            aiod.ToString());
                    }
                    else
                    {
                        dec = string.Format("  set_standard_digital_out({0}, {1})",
                            aiod.pin,
                            aiod.on ? "True" : "False");
                    }
                    break;

                case ActionType.IOAnalog:
                    ActionIOAnalog aioa = (ActionIOAnalog)action;
                    if (aioa.pin < 0 || aioa.pin >= cursor.analogOutputs.Length)
                    {
                        dec = string.Format("   {0} ERROR on \"{1}\": IO number not available",
                            COMMENT_CHAR,
                            aioa.ToString());
                    }
                    else if (aioa.pin > 1)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": analog IO pin not available in UR robot",
                            COMMENT_CHAR,
                            aioa.ToString());
                    }
                    else if (aioa.value < 0 || aioa.value > 1)
                    {
                        dec = string.Format("  {0} ERROR on \"{1}\": value out of range [0.0, 1.0]",
                            COMMENT_CHAR,
                            aioa.ToString());
                    }
                    else
                    {
                        dec = string.Format("  set_standard_analog_out({0}, {1})",
                            aioa.pin,
                            Math.Round(aioa.value, Geometry.STRING_ROUND_DECIMALS_VOLTAGE));
                    }
                    break;

                    //default:
                    //    dec = string.Format("  # ACTION \"{0}\" NOT IMPLEMENTED", action);
                    //    break;
            }

            if (humanComments && action.type != ActionType.Comment)
            {
                dec = string.Format("{0}  {1} [{2}]",
                    dec,
                    COMMENT_CHAR,
                    action.ToString());
            }
            //else if (ADD_ACTION_ID)
            //{
            //    dec = string.Format("{0}  {1} [{2}]",
            //        dec,
            //        COMMENT_CHAR,
            //        action.id);
            //}

            declaration = dec;
            return dec != null;
        }




        /// <summary>
        /// Returns an UR pose representation of the current state of the cursor.
        /// </summary>
        /// <returns></returns>
        internal static string GetPoseTargetValue(RobotCursor cursor)
        {
            RotationVector axisAng = cursor.rotation.GetRotationVector(true);
            return string.Format("p[{0},{1},{2},{3},{4},{5}]",
                Math.Round(0.001 * cursor.position.X, Geometry.STRING_ROUND_DECIMALS_M),
                Math.Round(0.001 * cursor.position.Y, Geometry.STRING_ROUND_DECIMALS_M),
                Math.Round(0.001 * cursor.position.Z, Geometry.STRING_ROUND_DECIMALS_M),
                Math.Round(axisAng.X, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(axisAng.Y, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(axisAng.Z, Geometry.STRING_ROUND_DECIMALS_RADS));
        }

        /// <summary>
        /// Returns a UR joint representation of the current state of the cursor.
        /// </summary>
        /// <returns></returns>
        internal static string GetJointTargetValue(RobotCursor cursor)
        {
            Joints jrad = new Joints(cursor.joints);  // use a shallow copy
            jrad.Scale(Geometry.TO_RADS);  // convert to radians
            return string.Format("[{0},{1},{2},{3},{4},{5}]",
                Math.Round(jrad.J1, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(jrad.J2, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(jrad.J3, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(jrad.J4, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(jrad.J5, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(jrad.J6, Geometry.STRING_ROUND_DECIMALS_RADS));
        }

        /// <summary>
        /// Returns a UR representation of a Tool object.
        /// </summary>
        /// <param name="cursor"></param>
        /// <returns></returns>
        internal static string GetToolValue(RobotCursor cursor)  //TODO: wouldn't it be just better to pass the Tool object? Inconsistent with the rest of the API...
        {
            if (cursor.tool == null)
            {
                throw new Exception("Cursor has no tool attached");
            }

            RotationVector axisAng = cursor.tool.TCPOrientation.Q.ToRotationVector(true);

            return string.Format("p[{0},{1},{2},{3},{4},{5}]",
                Math.Round(0.001 * cursor.tool.TCPPosition.X, Geometry.STRING_ROUND_DECIMALS_M),
                Math.Round(0.001 * cursor.tool.TCPPosition.Y, Geometry.STRING_ROUND_DECIMALS_M),
                Math.Round(0.001 * cursor.tool.TCPPosition.Z, Geometry.STRING_ROUND_DECIMALS_M),
                Math.Round(axisAng.X, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(axisAng.Y, Geometry.STRING_ROUND_DECIMALS_RADS),
                Math.Round(axisAng.Z, Geometry.STRING_ROUND_DECIMALS_RADS));

        }

    }
}
