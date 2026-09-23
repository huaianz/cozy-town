mergeInto(LibraryManager.library, {
  MiniGame_IsAvailable: function () {
    if (typeof wx !== 'undefined' && wx.setStorageSync) return 1;
    if (typeof tt !== 'undefined' && tt.setStorageSync) return 1;
    return 0;
  },

  MiniGame_GetStorage: function (keyPtr) {
    var key = UTF8ToString(keyPtr);
    var value = "";

    if (typeof wx !== 'undefined' && wx.getStorageSync) {
      value = wx.getStorageSync(key) || "";
    } else if (typeof tt !== 'undefined' && tt.getStorageSync) {
      value = tt.getStorageSync(key) || "";
    }

    if (typeof value !== 'string') {
      value = "";
    }

    var size = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(size);
    stringToUTF8(value, buffer, size);

    return buffer;
  },

  MiniGame_SetStorage: function (keyPtr, valuePtr) {
    var key = UTF8ToString(keyPtr);
    var value = UTF8ToString(valuePtr);

    if (typeof wx !== 'undefined' && wx.setStorageSync) {
      wx.setStorageSync(key, value);
    } else if (typeof tt !== 'undefined' && tt.setStorageSync) {
      tt.setStorageSync(key, value);
    }
  },

  MiniGame_DeleteStorage: function (keyPtr) {
    var key = UTF8ToString(keyPtr);

    if (typeof wx !== 'undefined' && wx.removeStorageSync) {
      wx.removeStorageSync(key);
    } else if (typeof tt !== 'undefined' && tt.removeStorageSync) {
      tt.removeStorageSync(key);
    }
  },

  MiniGame_Share: function (titlePtr) {
    var title = UTF8ToString(titlePtr);

    if (typeof wx !== 'undefined' && wx.shareAppMessage) {
      wx.shareAppMessage({ title: title });
      return 1;
    }

    if (typeof tt !== 'undefined' && tt.shareAppMessage) {
      tt.shareAppMessage({ title: title });
      return 1;
    }

    return 0;
  },

  MiniGame_Vibrate: function (milliseconds) {
    if (typeof wx !== 'undefined' && wx.vibrateShort) {
      wx.vibrateShort({ type: milliseconds > 30 ? 'medium' : 'light' });
    } else if (typeof tt !== 'undefined' && tt.vibrateShort) {
      tt.vibrateShort();
    }
  }
});