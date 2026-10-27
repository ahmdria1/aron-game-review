// Native side of Aron's haptics on iOS. Called from Scripts/Feedback/HapticService.cs through [DllImport("__Internal")].
// Impact styles: 0 light, 1 medium, 2 heavy. Notification types: 0 success, 1 warning, 2 error.
#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>

static UIImpactFeedbackGenerator *aronLight, *aronMedium, *aronHeavy;
static UINotificationFeedbackGenerator *aronNotify;

extern "C" {
    void AronHapticPrepare(void) {
        if (@available(iOS 10.0, *)) {
            if (!aronLight) {
                aronLight = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleLight];
                aronMedium = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleMedium];
                aronHeavy = [[UIImpactFeedbackGenerator alloc] initWithStyle:UIImpactFeedbackStyleHeavy];
                aronNotify = [[UINotificationFeedbackGenerator alloc] init];
            }
            [aronLight prepare]; [aronMedium prepare]; [aronHeavy prepare]; [aronNotify prepare];
        }
    }

    // intensity 0..1 scales the strength on iOS 13+; older versions ignore it.
    void AronHapticImpact(int style, float intensity) {
        if (@available(iOS 10.0, *)) {
            AronHapticPrepare();
            UIImpactFeedbackGenerator *g = style == 0 ? aronLight : style == 2 ? aronHeavy : aronMedium;
            if (@available(iOS 13.0, *)) [g impactOccurredWithIntensity:intensity];
            else [g impactOccurred];
        }
    }

    void AronHapticNotification(int type) {
        if (@available(iOS 10.0, *)) {
            AronHapticPrepare();
            UINotificationFeedbackType t = type == 1 ? UINotificationFeedbackTypeWarning : type == 2 ? UINotificationFeedbackTypeError : UINotificationFeedbackTypeSuccess;
            [aronNotify notificationOccurred:t];
        }
    }

    // 1 when Low Power Mode is on (the iOS counterpart of Android battery saver).
    int AronHapticLowPower(void) {
        return [NSProcessInfo processInfo].isLowPowerModeEnabled ? 1 : 0;
    }
}
