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
        public static void enableMotorA(bool forwardA, bool backwardA, bool option)
        {
            try
            {
                if (forwardA)
                {
                    if (option)
                        StaticComponents.directionForwardAPin.Write(GpioPinValue.High);
                    else
                        StaticComponents.directionForwardAPin.Write(GpioPinValue.Low);
                }

                if (backwardA)
                {
                    if (option)
                        StaticComponents.directionBackwardAPin.Write(GpioPinValue.High);
                    else
                        StaticComponents.directionBackwardAPin.Write(GpioPinValue.Low);
                }
            }
            catch (Exception)
            {
            }
            
        }

        public static void enableMotorB(bool forwardB, bool backwardB, bool option)
        {
            try
            {
                if (forwardB)
                {
                    if (option)
                        StaticComponents.directionForwardBPin.Write(GpioPinValue.High);
                    else
                        StaticComponents.directionForwardBPin.Write(GpioPinValue.Low);
                }

                if (backwardB)
                {
                    if (option)
                        StaticComponents.directionBackwardBPin.Write(GpioPinValue.High);
                    else
                        StaticComponents.directionBackwardBPin.Write(GpioPinValue.Low);
                }
            }
            catch (Exception)
            {
                
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
