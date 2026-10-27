// (c) Ahmed Alamrani. All rights reserved.
#include <time.h>

// CLOCK_MONOTONIC keeps counting while the phone sleeps, and the player cannot set it.
extern "C" long long AronMonotonicMs(void) {
    return (long long)(clock_gettime_nsec_np(CLOCK_MONOTONIC) / 1000000ULL);
}
