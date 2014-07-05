/*
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Dargon.Extensions;
using ItzWarty;

namespace Dargon.DargonServiceProtocol
{
   /// <summary>
   /// A ISendTimeContext object encapsulates all methods used to communicate from the DSP Server
   /// to the DSP Client.
   /// </summary>
   public class DSPClientHandler : ISendTimeContext
   {
      //-------------------------------------------------------------------------------------------
      // Private Fields
      //-------------------------------------------------------------------------------------------
      private readonly NetworkStream m_networkStream;
      private readonly BinaryWriter m_writer;
      private readonly BinaryReader m_reader;

      private readonly Thread m_thread;

      //-------------------------------------------------------------------------------------------
      // Public Methods
      //-------------------------------------------------------------------------------------------
      /// <summary>
      /// Constructor for a Dargon Service Protocol Client Session Handler.
      /// A ISendTimeContext object encapsulates all methods used to communicate from the
      /// DSP Server to the DSP Client.
      /// </summary>
      /// <param name="conn">
      /// The underlying socket which provides communication between the DSP Server and the client.
      /// </param>
      public DSPClientHandler(Socket conn)
      {
         m_networkStream = new NetworkStream(conn);
         m_writer = new BinaryWriter(m_networkStream);
         m_reader = new BinaryReader(m_networkStream);
         m_thread = new Thread(HandleClient);
      }

      /// <summary>
      /// Starts the main thread of this DSP Client Handler.
      /// </summary>
      public void BeginRun()
      {
         m_thread.BeginAggregate();
      }

      /// <summary>
      /// Sends a RAF Override to the DSP Client.  This method may only be invoked during Dargon'conn
      /// SendTime.
      /// </summary>
      /// <param name="rafPath">Path of the RAF Override, without RAFID</param>
      /// <param name="content">Content of the RAF Override</param>
      public void SendRAFOverride(string rafPath, byte[] content)
      {
         DSPServer.DargonService.WriteLine("Sending file " + rafPath);
         byte[] rafPathBuffer = new byte[512];
         byte[] rafPathBytes = Encoding.ASCII.GetBytes(rafPath);
         Array.Copy(rafPathBytes, rafPathBuffer, rafPath.Length);

         m_writer.Write((byte)DSP.DSP_CONTINUE);
         m_writer.Write(rafPathBuffer, 0, rafPathBuffer.Length);
         m_writer.Write((uint)content.Length);
         m_writer.Write(content, 0, content.Length);
         m_writer.Flush();
         DSPServer.DargonService.WriteLine("Sent");
      }

      private void HandleClient()
      {
         string outputPath = "C:/dargonnetdump/" + DateTime.Now.Day + "_" + DateTime.Now.Month + "_" + DateTime.Now.Year + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second + ".dmd";
         Util.PrepareParentDirectory(outputPath);
         FileStream dspDumpFs = File.Create(outputPath);
         BinaryWriter dspDumpFsWriter = new BinaryWriter(dspDumpFs);
         using (var timer = new UsingTimer((t) => DSPServer.DargonService.WriteLine("Session with Dargon Client ended after {0} seconds".F(t))))
         {
            byte[] lastDspNetOut = null;
            bool done = false;
            while (true)
            {
               byte opcode = m_reader.ReadByte();
               switch ((DSP)opcode)
               {
                  case DSP.DSP_WRITELINE:
                  {
                     uint textLength = m_reader.ReadUInt32();
                     byte[] textBytes = m_reader.ReadBytes((int)textLength);
                     string text = Encoding.ASCII.GetString(textBytes);
                     DSPServer.DargonService.WriteLine(text);
                     break;
                  }
                  case DSP.DSP_GET_DARGON_VERSION:
                  {
                     m_writer.Write(Core.Version.DargonApplicationInformation.VersionBytes, 0, 5);
                     m_writer.Flush();
                     break;
                  }
                  case DSP.DSP_GET_STATIC_MODIFICATIONS:
                  {
                     DSPServer.DargonService.WriteLine("** GET STATIC MODIFICATIONS **");
                     //TODO: OPTIMIZE
                     Type<DargonModification> recursion = null;
                     recursion = (mod) =>
                     {
                        DSPServer.DargonService.WriteLine("Applying mod " + mod.Name);
                        if (!mod.Sent)
                           mod.Apply(this);

                        foreach (var child in mod.EnumerateChildrenModifications())
                           recursion(child);
                     };
                     var modifications = DSPServer.DargonService.GetModificationManager().EnumerateTopLevelModifications();
                     modifications.ToList().ForEach(recursion);
                     DSPServer.DargonService.WriteLine("Sending Done");
                     m_writer.Write((byte)DSP.DSP_DONE);
                     m_writer.Flush();
                     DSPServer.DargonService.WriteLine("DSP_GSM Done");
                     break;
                  }
                  case DSP.DSP_ON_RESOURCE_READ:
                  {
                     byte[] fullRafPath = m_reader.ReadBytes(512); //[rafid]/[rafpath]
                     uint fileOffset = m_reader.ReadUInt32();
                     uint fileSize = m_reader.ReadUInt32();
                     DSPServer.DargonService.WriteLine("Dargon.LoL reading " + fullRafPath);
                     break;
                  }
                  case DSP.DSP_NET_RECV:
                  {
                     uint flags = m_reader.ReadUInt32();
                     uint bufferCount = m_reader.ReadUInt32();
                     for (uint i = 0; i < bufferCount; i++)
                     {
                        uint bufferSize = m_reader.ReadUInt32();
                        byte[] buffer = m_reader.ReadBytes((int)bufferSize);

                        if (lastDspNetOut == null)
                        {
                           lastDspNetOut = buffer;
                           dspDumpFsWriter.Write((UInt32)timer.ElapsedTime.TotalMilliseconds);
                           dspDumpFsWriter.Write((UInt32)bufferSize);
                           dspDumpFsWriter.Write(buffer, 0, (int)bufferSize);
                        }
                        else
                        {
                           bool equal = true;
                           if (buffer.Length == lastDspNetOut.Length)
                           {
                              for (int lambda = 0; lambda < buffer.Length; lambda++)
                              {
                                 if (buffer[lambda] != lastDspNetOut[lambda])
                                 {
                                    equal = false;
                                    break;
                                 }
                              }
                           }
                           else
                           {
                              equal = false;
                           }

                           if (!equal)
                           {
                              dspDumpFsWriter.Write((UInt32)timer.ElapsedTime.TotalMilliseconds);
                              dspDumpFsWriter.Write((UInt32)bufferSize);
                              dspDumpFsWriter.Write(buffer, 0, (int)bufferSize);
                              lastDspNetOut = buffer;
                           }
                        }
                     }
                     //DargonService.WriteLine("Dargon.LoL DSP_NET_RECV got " + bufferCount + " buffers");
                     break;
                  }
                  case DSP.DSP_GOODBYE:
                  {
                     m_writer.Write((byte)DSP.DSP_OK);
                     m_writer.Flush();
                     break;
                  }
                  default:
                  {
                     DSPServer.DargonService.WriteLine("Unknown Opcode: " + opcode);
                     break;
                  }
               }
            }
         }
      }
   }
}*/