using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace VirtualMemory
{        

    class Program
    {
        private static List<Page> PageTable = new List<Page>(256);
        private static Stack<Page> TLB = new Stack<Page>(16);
        private static List<SByte> BackingStore = new List<SByte>();
        private static List<string> ReadINVals = new List<string>();
        private static List<SByte[]> MainMemory = new List<SByte[]>(128);
      
    static void Main(string[] args)
        {
            Console.Title = "VIRTUAL MEMORY PROJECT";
            //virtual addr apace =2^16 65536
            //page size 2^8 =256
            //16 entries n TLB
            //2^8 entries in page table=256
            //128 page frames
            //physcial memory 32,768 (128 frames*256 byte frame size)
            //virtual memort 65,536
            //num frames =256
            //tlb size = 16
            //define page table size = 256
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Place file.txt, address.txt, and BACKING_STORE.bin on on your desktop then hit  enter.  This is so there is no issues readin the files using the meothods im using. If you dont name them correctly or place them on your desktop im gonna exit on you.");
            Console.ReadKey();    
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Reading Files ...");
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\file.txt";
                string path2 = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\address.txt";
                string path3 = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\BACKING_STORE.bin";
            Console.WriteLine("Good file paths :) Starting read in...");
            ReadInBackingStore(path3);//making backing store act like actual memory
            MakePageTable();//builds empty page table
            ReadINVals=File.ReadAllLines(path2).ToList();//reads in the address file
            Console.WriteLine("Finished read in...");
            Console.ForegroundColor = ConsoleColor.White;
            int frame = 0;

            double TLBHit = 0;
            double TLBMiss = 0;
            double PTHit = 0;
            double PTMiss = 0;

            for (int x=0; x<ReadINVals.Count;++x)
            {
                int VAddr = Convert.ToInt32(ReadINVals.ElementAt(x));
                int offset = GetPageOffset(VAddr);
                int pagenum = GetPageNum(VAddr);
                int framespot = 0;
                if (CheckTLB(pagenum, frame)==1)
                {
                    int temp = GetTLBElemNumber(pagenum, frame);
                    if (temp !=-2 && temp!=-1)
                    {
                        framespot = TLB.ElementAt(temp).Framenum;
                        ++TLBHit;
                    }
                    else if (CheckPageTable(pagenum))
                    {
                        framespot = PageTable.ElementAt(pagenum).Framenum;
                        ++TLBMiss;
                        ++PTHit;
                    }
                    else
                    { 
                        PageTable.ElementAt(pagenum).Valid = true;
                        PageTable.ElementAt(pagenum).Framenum = frame;
                        PageTable.ElementAt(pagenum).Pagenum = pagenum;
                        ++TLBMiss;
                        ++PTMiss;
                    }
                }
                else if (CheckPageTable(pagenum))
                {
                    framespot = PageTable.ElementAt(pagenum).Framenum;
                    ++TLBMiss;
                    ++PTHit;
                }
                else 
                {
                    PageTable.ElementAt(pagenum).Valid = true;
                    PageTable.ElementAt(pagenum).Framenum = frame;
                    PageTable.ElementAt(pagenum).Pagenum = pagenum;
                    ++TLBMiss;
                    ++PTMiss;
                }
                if (framespot != 0)
                {
                    Console.WriteLine((x + 1) + ". " + "VADDR:" + VAddr + " PADDR:" + ((framespot << 8) | offset) + " Value:" + BackingStore.ElementAt(VAddr));
                }
                else
                {
                    Console.WriteLine((x + 1) + ". " + "VADDR:" + VAddr + " PADDR:" + ((frame << 8) | offset) + " Value:" + BackingStore.ElementAt(VAddr));
                    ++frame;
                }    
                if (x%288==0&&x!=0)
                {
                    Console.WriteLine("\n\nSo here hit enter to skip.  You will only have to do this 4 total times.  I just have this here for you to view data before it leaves the console.");
                    Console.ReadKey();
                }
            }
            Console.WriteLine("\nTLB policy=FIFO");
            Console.WriteLine("\nTLB HIT:" + TLBHit + " TLB Miss:" + TLBMiss + " Ratio:"+(TLBHit*100/((TLBHit+TLBMiss)*100))+"\n" + "Page Hits:" + PTHit + " PageTable Miss:" + PTMiss + " Page Fault:"+(PTMiss*100/(PTHit+PTMiss)*100)+" Ratio:"+(PTHit/PTMiss));
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.Beep(2500, 2000);
                Console.WriteLine("SHUTTING DOWN, ERROR READING FILE, I TOLD YOU SO!");
                Console.ReadKey();
                Environment.Exit(0);
            }
            }//end main

    public static void MakePageTable()
        {
            Page page;
            for (int x = 0; x < MainMemory.Count+1; ++x)
            {
                page = new Page();
                page.Pagenum = x;
                page.Framenum = x;
                page.Valid = false;
                PageTable.Add(page);    
            }
        }

    public static int GetTLBElemNumber(int PageNum, int frame)
    {
        if (TLB.Count == 0)
        {
            AddtoTLB(PageNum, frame);
            return -2;
        }
        else
        {
            for (int x = 0; x < TLB.Count; ++x)
            {
                if (TLB.ElementAt(x).Pagenum == PageNum && TLB.ElementAt(x).Valid==true)
                {
                    return x;
                }
            }          
        }
        AddtoTLB(PageNum, frame);
        return -1;
    }

    public static void ReadInBackingStore(string path3)
    {
        using (BinaryReader b = new BinaryReader(File.Open(path3, FileMode.Open), Encoding.ASCII))
        {
            int pos = 0;
            Int64 length = (Int64)b.BaseStream.Length;
            while (pos < length)
            {
                SByte v = b.ReadSByte();
                BackingStore.Add(v);
                pos++;
            }
            SByte[] Frame = new SByte[257];
            int Y = 0;
            for (int x = 0; x < BackingStore.Count; ++x)
            {
                Frame[Y] = BackingStore.ElementAt(x);
                Y++;
                if (x % 256 == 0 && x != 0)
                {
                    MainMemory.Add(Frame);
                    Y = 0;
                    Frame = new SByte[256];
                }
            }
        }
    }

    public static void AddtoTLB(int PageNum, int frame)
    {
        if (TLB.Count < 16)
        {
            Page insertPage = new Page();
            insertPage.Pagenum = PageNum;
            insertPage.Framenum = frame;
            insertPage.Valid = true;
            TLB.Push(insertPage);
        }
        else
        {
            TLB.Pop();
            AddtoTLB(PageNum, frame);
        }
    }

    public static int CheckTLB(int PageNum, int frame)
    {
        if (TLB.Count==0)
        {
            AddtoTLB(PageNum, frame);
            return 0;
        }
        else
        {
            for (int x=0;x<TLB.Count;++x)
            {
                if (TLB.ElementAt(x).Pagenum == PageNum && TLB.ElementAt(x).Valid==true)
             {
                 return 1;
             }
            }
        }
        AddtoTLB(PageNum, frame);
        return 0;
    }

    public static bool CheckPageTable(int pagenum)
    {
            if (PageTable.ElementAt(pagenum).Valid)
            {
               return true;
            }
        else
            {
               return false;
            }
    }

    public static int GetPageOffset(int VAddr)
    {
        return VAddr = VAddr & 255;
    }

    public static int GetPageNum(int VAddr)
    {
        int PageNum = VAddr >> 8;
        return PageNum;
    }

    }
  
    public class Page
    {
        public int Pagenum=0;
        public int Framenum=0;
        public bool Valid =false;
    }
}
