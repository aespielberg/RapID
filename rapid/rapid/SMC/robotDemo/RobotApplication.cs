using MathNet.Numerics.Distributions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using RFID;

namespace smc
{
  public class RobotApplication<TState, TInteraction> : RFIDApplication<TState, TInteraction> where TInteraction : RobotInteraction<TState>, new() where TState : RobotInteractionState
  {

    private bool sending = false; // True if we are waiting for robot to ack
    private char[] servoCommands; // Sequence of commands to walk the robot


    public RobotApplication() : base()
    {
      this.initialize();
    }

    private void initialize()
    {
      // TODO
    }

    public override void afterInitialize()
    {
      Console.WriteLine("afterInitialize in RobotApplication");
            base.afterInitialize();
      // TODO: start some stuff?
    }

    // Called after every data update.
    // Checks state, see if we should begin transmission or wait and do nothing.
    public override void afterUpdate()
    {
      // TODO: get most likely state
      // Check if we are currently sending something or not.
    }
  }
}
