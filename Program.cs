using System;

namespace BitDecodeTest
{
    public class Program
    {
        public static void Main(string[] args)
        {
            byte[] data = { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };

            /* MSBを0スタートに変更（デフォルトは7スタート) */
            BitDecoder.MsbMode = BitDecoder.EMsbMode.MSB_0_START;

            /* MSGが7スタートの場合(各バイトの一番左のビットインデックスが7) */
            if (BitDecoder.MsbMode == BitDecoder.EMsbMode.MSB_7_START)
            {
                BitDecodeTestUInt8(data, 1, 5, 4);

                BitDecodeTestUInt8(data, 3, 5, 5);

                BitDecodeTestUInt16(data, 4, 5, 12);

                BitDecodeTestUInt32(data, 1, 3, 30);

                BitDecodeTestUInt32(data, 3, 2, 21);

                BitDecodeTestUInt64(data, 0, 7, 50);

                BitDecodeTestUInt64(data, 0, 7, 64);
            }
            /* MSGが0スタートの場合(各バイトの一番左のビットインデックスが0) */
            else
            {
                BitDecodeTestUInt8(data, 1, 2, 4);

                BitDecodeTestUInt8(data, 3, 2, 5);

                BitDecodeTestUInt16(data, 4, 2, 12);

                BitDecodeTestUInt32(data, 1, 7, 30);

                BitDecodeTestUInt32(data, 3, 5, 21);

                BitDecodeTestUInt64(data, 0, 0, 50);

                BitDecodeTestUInt64(data, 0, 0, 64);
            }
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

        /* MSBモード */
        public enum EMsbMode
        {
            /* MSB:7 to LSB:0 */
            MSB_7_START,
            /* MSB:0 to LSB:7 */
            MSB_0_START,
        }

        /* MSBモード */
        public static EMsbMode MsbMode { get; set; } = EMsbMode.MSB_0_START;

        /* バイトあたりのビットサイズ */
        private const int ByteBitSize = 8;

        /* バイトあたりの最大ビットインデックス : バイトあたりのビットサイズ - 1 */
        private const int MaxByteBitIndex = ByteBitSize - 1;

        /* 最小バイトサイズリミット : byte型のサイズ */
        private const int MinByteSizeLimit = sizeof(byte);

        /* 最大バイトサイズリミット : ulong型のサイズ */
        private const int MaxByteSizeLimit = sizeof(ulong);

        /* 最小ビットサイズ : 1 bit */
        private const int MinBitSize = 1;

        /* 最大ビットサイズ : 最大バイトサイズリミット * バイトあたりのビットサイズ */
        private const int MaxBitSize = MaxByteSizeLimit * ByteBitSize;

        /* 最大マスク : ulongの最大値 */
        private const ulong MaskMax = ulong.MaxValue;

        /* バイト配列の指定オフセット位置、指定ビット位置から、指定ビットサイズ分データを抽出 */
        public static ulong DecodeBits(byte[] data, ref int offset, int start_bit, int bit_size, int max_byte_size)
        {
            // 引数のチェック
            if (data == null || data.Length == 0)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0 || offset >= data.Length)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (start_bit < 0 || start_bit > MaxByteBitIndex)
                throw new ArgumentOutOfRangeException(nameof(start_bit));
            if (bit_size < MinBitSize || bit_size > MaxBitSize)
                throw new ArgumentOutOfRangeException(nameof(bit_size));
            if (max_byte_size < MinByteSizeLimit || max_byte_size > MaxByteSizeLimit)
                throw new ArgumentOutOfRangeException(nameof(max_byte_size));

            /* Byte     : 0               1               2               3               4               5... */
            /* Bit      : 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0 7 6 5 4 3 2 1 0 7... */
            /* Bit(inv) : 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0... */

            /* MSBが0スタートの場合 */
            if (BitDecoder.MsbMode == EMsbMode.MSB_0_START)
            {
                /* Bitを0スタートにしたい場合は、開始ビットを反転(7 to 0 => 0 to 7) */
                start_bit = BitDecoder.InvertBitIndex(start_bit);
            }

            /* 開始ビットを反転(7 to 0 => 0 to 7) */
            int start_bit_inv = BitDecoder.InvertBitIndex(start_bit);

            /* 反転終了ビットインデックス : 反転開始ビット + ビットサイズ - 1 */
            int end_bit_inv_index = (start_bit_inv + bit_size - 1);

            /* すべてのビットが開始バイトに収まっているかを確認 ： 反転終了ビットインデックスが最大ビットインデックス以下であれば収まっている */
            if (end_bit_inv_index <= MaxByteBitIndex)
            {
                /* 開始バイトを抽出 */
                byte extract_data = data[offset];

                /* 終了ビットインデックス : 反転終了ビットインデックスを反転(0 to 7 => 7 to 0) */
                int end_bit_index = BitDecoder.InvertBitIndex(end_bit_inv_index);

                /* デコード値 : 抽出データをulongにキャストし、シフトサイズ分右シフトし、ビットマスク */
                ulong result = ExtractBits((ulong)extract_data, bit_size, end_bit_index);

                /* 反転終了ビットインデックスが7の場合、バイト終端まで読んだため、オフセットを加算、それ以外はまだバイト終端まで読んでいないためオフセット位置を保持 */
                if (end_bit_inv_index == MaxByteBitIndex)
                    offset++;

                return result;
            }
            /* 次のバイト以降をまたいでいる場合 */
            else
            {
                /* 抽出するバイトサイズ 、開始バイトのビットサイズ 、1バイトまるごと使用するバイトサイズ、終了バイトのビットサイズを算出, オフセットサイズを算出 */
                int extract_byte_size = BitDecoder.CalculateExtractByteSize(start_bit, bit_size, out int first_byte_bit_size, out int all_bits_byte_size, out int last_byte_bit_size, out int offset_size);

                /* 抽出バイトサイズチェック */
                if ((offset + extract_byte_size - 1) >= data.Length)
                    throw new InvalidOperationException($"Out of Range : offset={offset} extract_bytes={extract_byte_size} data_len{data.Length}");

                /* 開始ビット、終了ビットを含むバイト配列を抽出 : オフセット位置から、抽出バイトサイズ分 */
                byte[] extract_bytes = BitDecoder.Extract(data, offset, extract_byte_size);

                /* ulong変換用のパディングを追加したバイト配列(8byte)を生成 */
                byte[] tmp_bytes = ExtractWithPaddingOffset(extract_bytes, 0, MaxByteSizeLimit);

                /* バイト配列をulongに変換 : ビッグエンディアンであればそのまま変換、リトルエンディアンであれば反転して変換 */
                ulong extract_data = (!BitConverter.IsLittleEndian)
                                    ? BitConverter.ToUInt64(tmp_bytes, 0)
                                    : BitConverter.ToUInt64(tmp_bytes.Reverse().ToArray(), 0);

                /* 終了ビットインデックス : 終了バイトのビットサイズが0(バイトの終端)の場合は0、それ以外の場合はバイトあたりのビットサイズ - 終了バイトのビットサイズ */
                int last_bit_index = (last_byte_bit_size == 0) ? 0 : (ByteBitSize - last_byte_bit_size);

                /* デコード値 : ビットサイズと最終ビットインデックスを指定してビットデータを抽出(シフト&マスク) */
                ulong result = ExtractBits(extract_data, bit_size, last_bit_index);

                /* オフセットを加算 */
                offset += offset_size;

                return result;
            }
        }

        /* 抽出するバイトサイズ 、開始バイトのビットサイズ 、1バイトまるごと使用するバイトサイズ、終了バイトのビットサイズを算出, オフセットサイズを算出 */
        private static int CalculateExtractByteSize(int start_bit, int bit_size, out int first_byte_bit_size, out int all_bits_byte_size, out int last_byte_bit_size,  out int offset_size)
        {
            /* 開始位置のバイトは必ず抽出 */
            int start_byte_size = 1;

            /* 開始バイトのビットサイズ : 開始ビット + 1 */
            first_byte_bit_size = start_bit + 1;

            /* 残りビットサイズ : 合計ビットサイズ - 開始バイトのビットサイズ */
            int remain_bit_size = bit_size - first_byte_bit_size;

            /* 1バイトまるごと使用するバイトサイズ : 残りビットサイズ ÷ バイトあたりのビットサイズ */
            all_bits_byte_size = remain_bit_size / ByteBitSize;

            /* 終了バイトのビットサイズ : 残りビットサイズ ÷ バイトあたりのビットサイズの剰余 */
            last_byte_bit_size = remain_bit_size % ByteBitSize;

            /* 終了ビット位置のバイトサイズ : 剰余があれば加算、それ以外は加算しない */
            int last_byte_size = (last_byte_bit_size > 0) ? 1 : 0;

            /* 抽出するバイトサイズ : 開始バイトサイズ(1byte) + 1バイトまるごと使用するバイトサイズ + 終了ビット位置のバイトサイズ(1 or 0byte) */
            int extract_byte_size = start_byte_size + all_bits_byte_size + last_byte_size;

            /* オフセットサイズ : 開始バイトサイズ(1byte) + 1バイトまるごと使用するバイトサイズ */
            offset_size = (start_byte_size + all_bits_byte_size);

            return extract_byte_size;
        }

        /* バイト配列の指定オフセット位置から指定の長さ分バイト配列を抽出 */
        private static byte[] Extract(byte[] src, int offset, int length)
        {
            /* 抽出用バイト配列生成 */
            byte[] dst = new byte[length];

            /* バイト配列抽出 */
            Array.Copy(src, offset, dst, 0, length);

            return dst;
        }

        /* 入力バイト配列の指定オフセット位置から末尾までのデータを、指定の長さのバイト配列にコピー(先頭に不足分のパディングを付加) */
        private static byte[] ExtractWithPaddingOffset(byte[] src, int offset, int length)
        {
            /* バイト配列抽出 */
            byte[] dst = new byte[length];

            /* パディングのバイトサイズ : 出力配列バイトサイズ - (入力バイト配列サイズ - オフセットサイズ) */
            int padding_byte_size = length - (src.Length - offset);

            /* バイト配列抽出(パディング分コピー開始位置をオフセット) */
            Array.Copy(src, offset, dst, padding_byte_size, src.Length);

            return dst;
        }

        /* ビットサイズからビットマスク値を算出 */
        private static ulong GetMask(int bit_size)
        {
            /* ビットマスク : ビットサイズが最大ビットサイズの場合はulongの最大値、それ以外は1をビットサイズ分シフトして1を減算 */
            return (bit_size == MaxBitSize) ? MaskMax : (ulong)((1ul << bit_size) - 1);
        }

        /* ビットインデックスを反転(7 to 0 <=> 0 to 7) */
        private static int InvertBitIndex(int bit_index)
        {
            /* バイトあたりの最大ビットインデックス - ビットインデックス */
            return MaxByteBitIndex - bit_index;
        }

        /* データからビットサイズと最終ビットインデックスを指定してビットデータを抽出(シフト&マスク) */
        private static ulong ExtractBits(ulong data, int bit_size, int last_bit_index)
        {
            /* ビットサイズからビットマスク値を算出 */
            ulong mask = BitDecoder.GetMask(bit_size);

            /* 抽出ビットデータ : データを最終ビットインデックス分右シフトし、ビットマスク */
            ulong result = (ulong)((data >> last_bit_index) & mask);

            return result;
        }
    }
}