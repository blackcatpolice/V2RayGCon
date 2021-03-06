﻿using Newtonsoft.Json.Linq;
using System;

namespace V2RayGCon.Services.ShareLinkComponents.VeeCodecs
{
    internal sealed class Vmess0a :
        VgcApis.BaseClasses.ComponentOf<VeeDecoder>,
        IVeeDecoder
    {
        Cache cache;

        public Vmess0a(Cache cache)
        {
            this.cache = cache;
        }

        #region properties

        #endregion

        #region public methods
        public string GetSupportedVersion() =>
           Models.VeeShareLinks.Vmess0a.SupportedVersion();

        public byte[] Config2Bytes(JObject config)
        {
            var vee = Config2Vee(config);
            return vee?.ToBytes();
        }

        public Tuple<JObject, JToken> Bytes2Config(byte[] bytes)
        {
            var veeLink = new Models.VeeShareLinks.Vmess0a(bytes);
            return VeeToConfig(veeLink);
        }

        #endregion

        #region private methods
        Models.VeeShareLinks.Vmess0a Config2Vee(JObject config)
        {
            var bs = Comm.ExtractBasicConfig(config, @"vmess", @"vnext", out bool isUseV4, out string root);

            if (bs == null)
            {
                return null;
            }

            var GetStr = Misc.Utils.GetStringByPrefixAndKeyHelper(config);

            var vmess = new Models.VeeShareLinks.Vmess0a(bs);
            var userInfoPrefix = root + ".settings.vnext.0.users.0";
            vmess.alterId = VgcApis.Misc.Utils.Str2Int(GetStr(userInfoPrefix, "alterId"));
            vmess.uuid = Guid.Parse(GetStr(userInfoPrefix, "id"));
            return vmess;
        }


        Tuple<JObject, JToken> VeeToConfig(Models.VeeShareLinks.Vmess0a vee)
        {
            if (vee == null)
            {
                return null;
            }

            var outVmess = cache.tpl.LoadTemplate("outbVeeVmess");
            outVmess["streamSettings"] = Comm.GenStreamSetting(cache, vee);
            var node = outVmess["settings"]["vnext"][0];
            node["address"] = vee.address;
            node["port"] = vee.port;
            node["users"][0]["id"] = vee.uuid;

            if (vee.alterId > 0)
            {
                node["users"][0]["alterId"] = vee.alterId;
            }

            var tpl = cache.tpl.LoadTemplate("tplImportVmess") as JObject;
            tpl["v2raygcon"]["alias"] = vee.alias;
            tpl["v2raygcon"]["description"] = vee.description;
            return new Tuple<JObject, JToken>(tpl, outVmess);
        }

        #endregion

        #region protected methods

        #endregion
    }
}
