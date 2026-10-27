// (c) Ahmed Alamrani. All rights reserved.
#import <Foundation/Foundation.h>
#import <Security/Security.h>

// Keeps the per-install save secret in the iOS Keychain, readable only after first unlock, never synced or backed up.
static NSDictionary* AronQuery(void) {
    return @{ (__bridge id)kSecClass: (__bridge id)kSecClassGenericPassword,
              (__bridge id)kSecAttrService: @"com.ahmedalamrani.aron.save",
              (__bridge id)kSecAttrAccount: @"install-secret" };
}

extern "C" int AronKeychainLoad(unsigned char* buffer, int capacity) {
    NSMutableDictionary* q = [AronQuery() mutableCopy];
    q[(__bridge id)kSecReturnData] = @YES;
    q[(__bridge id)kSecMatchLimit] = (__bridge id)kSecMatchLimitOne;
    CFTypeRef out = NULL;
    if (SecItemCopyMatching((__bridge CFDictionaryRef)q, &out) != errSecSuccess || out == NULL) return -1;
    NSData* d = (__bridge_transfer NSData*)out;
    if ((int)d.length > capacity) return -1;
    memcpy(buffer, d.bytes, d.length);
    return (int)d.length;
}

extern "C" int AronKeychainSave(const unsigned char* data, int length) {
    SecItemDelete((__bridge CFDictionaryRef)AronQuery());
    NSMutableDictionary* q = [AronQuery() mutableCopy];
    q[(__bridge id)kSecValueData] = [NSData dataWithBytes:data length:length];
    q[(__bridge id)kSecAttrAccessible] = (__bridge id)kSecAttrAccessibleAfterFirstUnlockThisDeviceOnly;
    return SecItemAdd((__bridge CFDictionaryRef)q, NULL) == errSecSuccess ? 1 : 0;
}
