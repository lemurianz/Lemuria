using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Shasta.Helpers
{
    class StaticMethods
    {
        // Move
        public static void enableInput(bool forward, bool backward, bool option)
        {
            if (forward)
            {
                if (option)
                    StaticComponents.directionForwardPin.Write(GpioPinValue.High);
                else
                    StaticComponents.directionForwardPin.Write(GpioPinValue.Low);
            }

            if (backward)
            {
                if (option)
                    StaticComponents.directionBackwardPin.Write(GpioPinValue.High);
                else
                    StaticComponents.directionBackwardPin.Write(GpioPinValue.Low);
            }
        }

        public static void enableTurn(bool left, bool right, bool option)
        {
            if (right)
            {
                if (option)
                    StaticComponents.turnLeftPin.Write(GpioPinValue.High);
                else
                    StaticComponents.turnLeftPin.Write(GpioPinValue.Low);
            }

            if (left)
            {
                if (option)
                    StaticComponents.turnRightPin.Write(GpioPinValue.High);
                else
                    StaticComponents.turnRightPin.Write(GpioPinValue.Low);
            }
        }

        // Sonar
        public static async Task SendPulse()
        {
            StaticComponents.triggerPin.Write(GpioPinValue.Low);   // ensure the trigger is off
            await Task.Delay(TimeSpan.FromSeconds(2));  // wait for the sensor to settle

            StaticComponents.triggerPin.Write(GpioPinValue.High);
            await Task.Delay(TimeSpan.FromMilliseconds(.01));
            StaticComponents.triggerPin.Write(GpioPinValue.Low);
        }

        public static double ReceivePulse()
        {
            var time = PulseIn(StaticComponents.echoPin, GpioPinValue.High, 20);

            var distance = Math.Round(time * 17150, 2);

            return distance;
        }

        private static double PulseIn(GpioPin pin, GpioPinValue value, int timeout)
        {
            StaticComponents.StopWatch.Restart();

            // Wait for pulse
            while (StaticComponents.StopWatch.ElapsedMilliseconds < timeout && pin.Read() != value) { }

            if (StaticComponents.StopWatch.ElapsedMilliseconds >= timeout)
            {
                StaticComponents.StopWatch.Stop();
                return 0;
            }
            StaticComponents.StopWatch.Restart();

            // Wait for pulse end
            while (StaticComponents.StopWatch.ElapsedMilliseconds < timeout && pin.Read() == value) { }

            StaticComponents.StopWatch.Stop();

            return StaticComponents.StopWatch.ElapsedMilliseconds < timeout ? StaticComponents.StopWatch.Elapsed.TotalSeconds : 0;
        }
    }
}
