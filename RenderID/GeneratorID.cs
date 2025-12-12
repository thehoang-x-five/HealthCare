using System;
using System.Security.Cryptography;
using System.Text;

namespace HealthCare.RenderID
{
    public static class GeneratorID
    {
        // Sinh chuỗi base36 từ byte[]
        private static string ToBase36(ReadOnlySpan<byte> bytes, int length)
        {
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            // chuyển 8 byte đầu thành ulong
            ulong value = BitConverter.ToUInt64(bytes);
            var sb = new StringBuilder();

            while (value > 0 && sb.Length < length)
            {
                var index = (int)(value % 36);
                sb.Insert(0, alphabet[index]);
                value /= 36;
            }

            // pad nếu thiếu
            while (sb.Length < length)
                sb.Insert(0, '0');

            return sb.ToString();
        }

        public static string New(string prefix, int length = 6)
        {
            Span<byte> buffer = stackalloc byte[8];
            RandomNumberGenerator.Fill(buffer);
            var code = ToBase36(buffer, length);
            return string.IsNullOrWhiteSpace(prefix) ? code : $"{prefix}{code}";
        }

        // Một số helper cụ thể cho entity
       

        public static string NewBenhNhanId() => New("BN", 7);
        public static string NewLichHenId() => New("LH", 7);
        public static string NewPhieuKhamLsId() => New("PKLS", 7);
        public static string NewPhieuKhamClsId() => New("PKCLS", 7);
        public static string NewHangDoiId() => New("HDQ", 7);
        public static string NewLuotKhamId() => New("LK", 7);
        public static string NewDonThuocId() => New("DT", 7);
        public static string NewHoaDonId() => New("HD", 7);
        public static string NewKetQuaDichVuId() => New("KQ", 7);
        public static string NewThongBaoId() => New("TB", 7);

    }
}
