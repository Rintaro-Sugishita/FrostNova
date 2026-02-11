using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT;

/// <summary>
/// Specifies the method that returns the save path for the settings file.
/// </summary>
/// <remarks>
/// This attribute should be applied to a static method that returns a string and takes no arguments. It is required to generate LoadSettings/SaveSettings methods.
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]

public class JsonSettingPathAttribute : Attribute
{
}
