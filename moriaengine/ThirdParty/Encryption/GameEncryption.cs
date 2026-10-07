
//using System;
//using System.Collections;
//using System.Security.Cryptography;

//namespace SteamEngine.Packets
//{
//    public class GameEncryption : IClientEncryption
//    {
//        private SteamEngine.Networking.TwofishEncryption engine;

//        private ushort recvPos; // Position in our CipherTable (Recv)
//        private byte sendPos; // Offset in our XOR Table (Send)
//        private byte[] cipherTable;
//        private byte[] xorData; // This table is used for encrypting the server->client stream

//        public GameEncryption(uint seed) 
//        {
//            cipherTable = new byte[0x100];

//            // Set up the crypt key
//            byte[] key = new byte[16];
//            key[0] = key[4] = key[8] = key[12] = (byte)((seed >> 24) & 0xff);
//            key[1] = key[5] = key[9] = key[13] = (byte)((seed >> 16) & 0xff);
//            key[2] = key[6] = key[10] = key[14] = (byte)((seed >> 8) & 0xff);
//            key[3] = key[7] = key[11] = key[15] = (byte)(seed & 0xff);

//            byte[] iv = new byte[0];
//            engine = new SteamEngine.Networking.TwofishEncryption(128, ref key, ref iv, CipherMode.ECB, SteamEngine.Networking.TwofishBase.EncryptionDirection.Decrypting);

//            // Initialize table
//            for ( int i = 0; i < 256; ++i )
//                cipherTable[i] = (byte)i;

//            sendPos = 0;

//            // We need to fill the table initially to calculate the MD5 hash of it
//            refreshCipherTable();

//            // Create a MD5 hash of the twofish crypt data and use it as a 16-byte xor table
//            // for encrypting the server->client stream.
//            MD5 md5 = new MD5CryptoServiceProvider();
//            xorData = md5.ComputeHash(cipherTable);
//        }

//        private void refreshCipherTable() 
//        {
//            uint[] block = new uint[4];

//            for (int i = 0; i < 256; i += 16)
//            {			
//                Buffer.BlockCopy(cipherTable, i, block, 0, 16);
//                engine.blockEncrypt(ref block);
//                Buffer.BlockCopy(block, 0, cipherTable, i, 16);
//            }

//            recvPos = 0;
//        }

//        public void serverEncrypt(byte[] buffer) 
//        {
//            //byte[] packet = new byte[length];
//            for ( int i = 0, n = buffer.Length; i < n; ++i ) 
//            {
//                buffer[i] = (byte)(buffer[i] ^ xorData[sendPos++]);
//                sendPos &= 0x0F; // Maximum Value is 0xF = 15, then 0xF + 1 = 0 again
//            }
//            //buffer = packet;
//        }

//        public void clientDecrypt(ref byte[] buffer, int length) 
//        {
//            for (int i = 0; i < length; ++i) 
//            {
//                // Recalculate table
//                if ( recvPos >= 0x100 )
//                {
//                    //byte[] tmpBuffer = new byte[0x100];
//                    refreshCipherTable();
//                }

//                // Simple XOR operation
//                buffer[i] ^= cipherTable[recvPos++];
//            }
//        }
//    }
//}
