// (c) Ahmed Alamrani. All rights reserved.
#import <Foundation/Foundation.h>

// ProcessInfo.thermalState: 0 nominal, 1 fair, 2 serious, 3 critical.
extern "C" int AronThermalState(void)
{
    return (int)[NSProcessInfo processInfo].thermalState;
}

extern "C" int AronLowPowerMode(void)
{
    return [NSProcessInfo processInfo].lowPowerModeEnabled ? 1 : 0;
}
