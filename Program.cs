using System;

namespace BitDecodeTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            byte[] data = { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };

            BitDecodeTestUInt8(data, 3, 5, 5);

            BitDecodeTestUInt16(data, 4, 5, 12);

            BitDecodeTestUInt32(data, 1, 3, 30);

            BitDecodeTestUInt64(data, 0, 7, 50);

            BitDecodeTestUInt64(data, 0, 7, 64);
        }

        public static void BitDecodeTestUInt8(byte[] data, int offset, int start_bit, int bit_size)
        {
            int tmp_offset = offset;
            byte result = BitDecoder.DecodeUInt8BE(data, ref tmp_offset, start_bit, bit_size);
            Console.WriteLine($"result = 0x{result:X1} offset = {tmp_offset}");
        }

        public static void BitDecodeTestUInt16(byte[] data, int offset, int start_bit, int bit_size)
        {
            int tmp_offset = offset;
            ushort result = BitDecoder.DecodeUInt16BE(data, ref tmp_offset, start_bit, bit_size);
            Console.WriteLine($"result = 0x{result:X2} offset = {tmp_offset}");
        }

        public static void BitDecodeTestUInt32(byte[] data, int offset, int start_bit, int bit_size)
        {
            int tmp_offset = offset;
            uint result = BitDecoder.DecodeUInt32BE(data, ref tmp_offset, start_bit, bit_size);
            Console.WriteLine($"result = 0x{result:X4} offset = {tmp_offset}");
        }

        public static void BitDecodeTestUInt64(byte[] data, int offset, int start_bit, int bit_size)
        {
            int tmp_offset = offset;
            ulong result = BitDecoder.DecodeUInt64BE(data, ref tmp_offset, start_bit, bit_size);
            Console.WriteLine($"result = 0x{result:X8} offset = {tmp_offset}");
        }
    }

    public class BitDecoder
    {
        public static byte DecodeUInt8BE(byte[] data, ref int offset, int start_bit, int bit_size)
        {
            return (byte)BitDecoder.DecodeBits(data, ref offset, start_bit, bit_size, sizeof(byte));
        }

        public static ushort DecodeUInt16BE(byte[] data, ref int offset, int start_bit, int bit_size)
        {
            return (ushort)BitDecoder.DecodeBits(data, ref offset, start_bit, bit_size, sizeof(ushort));
        }

        public static uint DecodeUInt32BE(byte[] data, ref int offset, int start_bit, int bit_size)
        {
            return (uint)BitDecoder.DecodeBits(data, ref offset, start_bit, bit_size, sizeof(uint));
        }

        public static ulong DecodeUInt64BE(byte[] data, ref int offset, int start_bit, int bit_size)
        {
            return (ulong)BitDecoder.DecodeBits(data, ref offset, start_bit, bit_size, sizeof(ulong));
        }

        public static ulong DecodeBits(byte[] data, ref int offset, int start_bit, int bit_size, int max_byte_size)
        {
            /* バイトあたりのビットサイズ */
            int byte_bit_size = 8;

            /* バイトあたりの最大ビットインデックス : バイトあたりのビットサイズ - 1 */
            int max_byte_bit_index = byte_bit_size - 1;

            /* 最小バイトサイズリミット : byte型のサイズ */
            int min_byte_size_limit = sizeof(byte);

            /* 最大バイトサイズリミット : ulong型のサイズ */
            int max_byte_size_limit = sizeof(ulong);

            /* 最小ビットサイズ : 1 bit */
            int min_bit_size = 1;

            /* 最大ビットサイズ : 最大バイトサイズリミット * バイトあたりのビットサイズ */
            int max_bit_size = max_byte_size_limit * byte_bit_size;

            /* 最大マスク : ulongの最大値 */
            ulong max_mask = ulong.MaxValue;

            // 引数のチェック
            if (data == null || data.Length == 0) throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length) throw new ArgumentOutOfRangeException(nameof(offset));
            if (start_bit < 0 || start_bit > max_byte_bit_index) throw new ArgumentOutOfRangeException(nameof(start_bit));
            if (bit_size < min_bit_size || bit_size > max_bit_size) throw new ArgumentOutOfRangeException(nameof(bit_size));
            if (max_byte_size < min_byte_size_limit || max_byte_size > max_byte_size_limit) throw new ArgumentOutOfRangeException(nameof(max_byte_size));

            /* Byte     : 0               1               2               3               4               5... */
            /* Bit      : 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0 7... */
            /* Bit(inv) : 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0... */

            /* 開始ビットを反転(7 to 0 => 0 to 7) */
            int start_bit_inv = max_byte_bit_index - start_bit;

            /* 反転終了ビットインデックス : 反転開始ビット + ビットサイズ - 1 */
            int end_bit_inv_index = (start_bit_inv + bit_size - 1);

            /* すべてのビットが開始バイトに収まっているかを確認 ： 反転終了ビットインデックスが最大ビットインデックス以下であれば収まっている */
            bool is_all_bits_in_byte = (end_bit_inv_index <= max_byte_bit_index) ? true : false;

            /* ビットマスク : ビットサイズが最大ビットサイズの場合はulongの最大値、それ以外は1をビットサイズ分シフトして1を減算 */
            ulong mask = (bit_size == max_bit_size) ? max_mask : (ulong)((1ul << bit_size) - 1);

            /* すべてのビットが開始バイトに収まっている場合 */
            if (is_all_bits_in_byte == true)
            {
                /* 開始バイトを抽出 */
                byte extract_data = data[offset];

                /* ビットシフトサイズ : 反転終了ビットインデックスを反転(0 to 7 => 7 to 0) */
                int bit_shift_size = max_byte_bit_index - end_bit_inv_index;

                /* デコード値 : 抽出データをulongにキャストし、シフトサイズ分右シフトし、ビットマスク */
                ulong result = (ulong)(((ulong)extract_data >> bit_shift_size) & mask);

                /* 反転終了ビットインデックスが7の場合、バイト終端まで読んだため、オフセットを加算、それ以外はまだバイト終端まで読んでいないためオフセット位置を保持 */
                offset += (end_bit_inv_index == max_byte_bit_index) ? 1 : 0;

                return result;
            }
            /* 次のバイト以降をまたいでいる場合 */
            else
            {
                /* 開始位置のバイトは必ず抽出 */
                int start_byte_size = 1;

                /* 開始バイトのビットサイズ : 開始ビット + 1 */
                int first_byte_bit_size = start_bit + 1;

                /* 残りビットサイズ : 合計ビットサイズ - 開始バイトのビットサイズ */
                int remain_bit_size = bit_size - first_byte_bit_size;

                /* 1バイトまるごと使用するバイトサイズ : 残りビットサイズ ÷ バイトあたりのビットサイズ */
                int all_bits_byte_size = remain_bit_size / byte_bit_size;

                /* 終了バイトのビットサイズ : 残りビットサイズ ÷ バイトあたりのビットサイズの剰余 */
                int last_byte_bit_size = remain_bit_size % byte_bit_size;

                /* 終了ビット位置のバイトサイズ : 剰余があれば加算、それ以外は加算しない */
                int last_byte_size = (last_byte_bit_size > 0) ? 1 : 0;

                /* 抽出するバイトサイズ : 開始バイトサイズ(1byte) + 1バイトまるごと使用するバイトサイズ + 終了ビット位置のバイトサイズ(1 or 0byte) */
                int extract_byte_size = start_byte_size + all_bits_byte_size + last_byte_size;

                /* 抽出バイトサイズチェック */
                if ((offset + extract_byte_size - 1) >= data.Length) throw new InvalidOperationException($"Out of Range : offset={offset} extract_bytes={extract_byte_size} data_len{data.Length}");

                /* 開始ビット、終了ビットを含むバイト配列を抽出 : オフセット位置から、抽出バイトサイズ分 */
                byte[] extract_bytes = data.Skip(offset).Take(extract_byte_size).ToArray();

                /* ulong変換用の配列を用意(8byte) */
                byte[] tmp_bytes = new byte[sizeof(ulong)];

                /* パディングのバイトサイズ : ulongのサイズ(8byte) - 抽出したバイト配列サイズ */
                int padding_byte_size = sizeof(ulong) - extract_bytes.Length;

                /* パディングを追加したバイト配列を生成 : 抽出したバイト配列を、パディングバイトサイズ位置からコピー */
                Array.Copy(extract_bytes, 0, tmp_bytes, padding_byte_size, extract_bytes.Length);

                /* バイト配列をulongに変換 : ビッグエンディアンであればそのままて変換、リトルエンディアンであれば反転して変換 */
                ulong extract_data = (!BitConverter.IsLittleEndian)
                                    ? BitConverter.ToUInt64(tmp_bytes, 0)
                                    : BitConverter.ToUInt64(tmp_bytes.Reverse().ToArray(), 0);

                /* ビットシフトサイズ : 終了バイトのビットサイズが0(バイトの終端)の場合はシフト不要、それ以外の場合はバイトあたりのビットサイズ - 終了バイトのビットサイズ */
                int bit_shift_size = (last_byte_bit_size == 0) ? 0 : (byte_bit_size - last_byte_bit_size);

                /* デコード値 : 抽出データをシフトサイズ分右シフトし、ビットマスク */
                ulong result = (ulong)((extract_data >> bit_shift_size) & mask);

                /* オフセットを加算 : 開始バイトサイズ(1byte) + 1バイトまるごと使用するバイトサイズ */
                offset += (start_byte_size + all_bits_byte_size);

                return result;
            }
        }
    }
}