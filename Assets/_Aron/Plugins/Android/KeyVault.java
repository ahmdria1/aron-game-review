// (c) Ahmed Alamrani. All rights reserved.
package com.ahmedalamrani.aron;

import android.content.Context;
import android.content.SharedPreferences;
import android.security.keystore.KeyGenParameterSpec;
import android.security.keystore.KeyProperties;
import android.util.Base64;
import java.security.KeyStore;
import javax.crypto.Cipher;
import javax.crypto.KeyGenerator;
import javax.crypto.SecretKey;
import javax.crypto.spec.GCMParameterSpec;

/** Keeps the per-install save secret wrapped by a non-exportable Android Keystore key. Called from Unity over JNI. */
public final class KeyVault {
    private static final String ALIAS = "aron_save_wrap";
    private static final String PREFS = "aron_vault";
    private static final String ITEM = "s";

    private static SecretKey wrapKey() throws Exception {
        KeyStore ks = KeyStore.getInstance("AndroidKeyStore");
        ks.load(null);
        if (!ks.containsAlias(ALIAS)) {
            KeyGenerator g = KeyGenerator.getInstance(KeyProperties.KEY_ALGORITHM_AES, "AndroidKeyStore");
            g.init(new KeyGenParameterSpec.Builder(ALIAS, KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                    .setBlockModes(KeyProperties.BLOCK_MODE_GCM)
                    .setEncryptionPaddings(KeyProperties.ENCRYPTION_PADDING_NONE)
                    .setKeySize(256)
                    .build());
            return g.generateKey();
        }
        return (SecretKey) ks.getKey(ALIAS, null);
    }

    /** The stored secret, or null when none was saved or it can't be unwrapped. */
    public static byte[] load(Context ctx) {
        try {
            SharedPreferences p = ctx.getSharedPreferences(PREFS, Context.MODE_PRIVATE);
            String v = p.getString(ITEM, null);
            if (v == null) return null;
            byte[] all = Base64.decode(v, Base64.NO_WRAP);
            byte[] iv = new byte[12];
            System.arraycopy(all, 0, iv, 0, 12);
            Cipher c = Cipher.getInstance("AES/GCM/NoPadding");
            c.init(Cipher.DECRYPT_MODE, wrapKey(), new GCMParameterSpec(128, iv));
            return c.doFinal(all, 12, all.length - 12);
        } catch (Exception e) {
            return null;
        }
    }

    public static boolean save(Context ctx, byte[] secret) {
        try {
            Cipher c = Cipher.getInstance("AES/GCM/NoPadding");
            c.init(Cipher.ENCRYPT_MODE, wrapKey());
            byte[] iv = c.getIV();
            byte[] ct = c.doFinal(secret);
            byte[] all = new byte[iv.length + ct.length];
            System.arraycopy(iv, 0, all, 0, iv.length);
            System.arraycopy(ct, 0, all, iv.length, ct.length);
            ctx.getSharedPreferences(PREFS, Context.MODE_PRIVATE).edit().putString(ITEM, Base64.encodeToString(all, Base64.NO_WRAP)).apply();
            return true;
        } catch (Exception e) {
            return false;
        }
    }
}
