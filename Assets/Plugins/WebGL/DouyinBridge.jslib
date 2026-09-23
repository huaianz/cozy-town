mergeInto(LibraryManager.library, {

  Douyin_IsMiniGame: function () {
    return (typeof tt !== 'undefined' && tt !== null) ? 1 : 0;
  },

  Douyin_GetAppId: function () {
    var id = "ttaef9d84430815fb407";
    var buffer = _malloc(id.length + 1);
    stringToUTF8(id, buffer, id.length + 1);
    return buffer;
  },

  Douyin_Login: function () {
    if (typeof tt === 'undefined') { return; }

    tt.login({
      success: function (res) {
        console.log('[抖音] 登录成功 code=' + res.code);
      },
      fail: function (err) {
        console.log('[抖音] 登录失败 ' + JSON.stringify(err));
      }
    });
  },

  Douyin_SaveStorage: function (keyPtr, valuePtr) {
    if (typeof tt === 'undefined') { return; }

    var key = UTF8ToString(keyPtr);
    var value = UTF8ToString(valuePtr);

    try {
      tt.setStorageSync(key, value);
    } catch (e) {
      console.log('[抖音] 存储失败 ' + e);
    }
  },

  Douyin_LoadStorage: function (keyPtr) {
    if (typeof tt === 'undefined') { return 0; }

    var key = UTF8ToString(keyPtr);
    var value = '';

    try {
      value = tt.getStorageSync(key);
    } catch (e) {
      value = '';
    }

    if (!value) { return 0; }

    var buffer = _malloc(lengthBytesUTF8(value) + 1);
    stringToUTF8(value, buffer, lengthBytesUTF8(value) + 1);
    return buffer;
  },

  Douyin_Share: function (titlePtr) {
    if (typeof tt === 'undefined' || !tt.shareAppMessage) { return; }

    var title = UTF8ToString(titlePtr);

    tt.shareAppMessage({
      title: title,
      success: function () { console.log('[抖音] 分享成功'); },
      fail: function (err) { console.log('[抖音] 分享失败 ' + JSON.stringify(err)); }
    });
  },

  Douyin_ShowRewardedAd: function (adUnitPtr) {
    if (typeof tt === 'undefined' || !tt.createRewardedVideoAd) { return 0; }

    var adUnit = UTF8ToString(adUnitPtr);

    if (!adUnit) { return 0; }

    try {
      var ad = tt.createRewardedVideoAd({ adUnitId: adUnit });

      ad.onClose(function (res) {
        console.log('[抖音] 激励视频结束 isEnded=' + (res && res.isEnded));
      });

      ad.load().then(function () { ad.show(); });
      return 1;
    } catch (e) {
      console.log('[抖音] 广告失败 ' + e);
      return 0;
    }
  },

  Douyin_StartRecord: function () {
    if (typeof tt === 'undefined' || !tt.getGameRecorderManager) { return; }

    try {
      var recorder = tt.getGameRecorderManager();
      recorder.start({ duration: 15 });
      console.log('[抖音] 开始录屏 15 秒');
    } catch (e) {
      console.log('[抖音] 录屏失败 ' + e);
    }
  },

  Douyin_StopRecord: function () {
    if (typeof tt === 'undefined' || !tt.getGameRecorderManager) { return; }

    try {
      var recorder = tt.getGameRecorderManager();
      recorder.stop();
      console.log('[抖音] 结束录屏');
    } catch (e) {
      console.log('[抖音] 结束录屏失败 ' + e);
    }
  }
});