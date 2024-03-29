﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using ZampLib.Business;
using System.Net.NetworkInformation;
using System.Net;
using System.Security.Principal;

namespace ZampLib
{
    public class ZampGUILib
    {
        public static string getval_from_appsetting(string pathprog)
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;

            for (int i = 0; i < appSettings.Count; i++)
            {
                if (pathprog.Equals(appSettings.GetKey(i)))
                {
                    return appSettings[i];
                }
                //Console.WriteLine("#{0} Key: {1} Value: {2}", i, appSettings.GetKey(i), appSettings[i]);
            }
            return null;
        }

        public static void printMsg_and_exit(string msg = "", bool bexit = false, System.Windows.Forms.Form f = null)
        {
            if(!string.IsNullOrEmpty(msg))
            {
                System.Windows.Forms.MessageBox.Show(msg);
            }

            if(bexit)
            {
                if(f != null)
                {
                    f.Close();
                }
                System.Windows.Forms.Application.Exit();
            }
        }

        public static List<string> getAllDB(string port)
        {
            List<string> tempList = new List<string>();

            var connString = "Server=127.0.0.1;User ID=root;Password=root;Database=mysql;port=" + port;

            using (var conn = new MySqlConnection(connString))
            {
                conn.Open();

                // Insert some data
                /*using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "INSERT INTO data (some_field) VALUES (@p)";
                    cmd.Parameters.AddWithValue("p", "Hello world");
                    cmd.ExecuteNonQuery();
                }*/

                // Retrieve all rows
                using (var cmd = new MySqlCommand("SHOW DATABASES;", conn))
                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                        tempList.Add(reader.GetString(0));
            }

            return tempList;
        }

        public static bool port_in_use(string _port, string _procid)
        {
            int port = Convert.ToInt32(_port);
            bool inUse = false;

            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    int proc_id = getProcessId_from_port(port);
                    if(proc_id.ToString() == _procid)
                    {
                        continue;
                    }
                    inUse = true;
                    break;
                }
            }

            return inUse;
        }

        public static string startProc(ConfigVar cv, typeProg tpg, string[] args)
        {
            string friendly_name = cv.get_friendly_name(tpg);
            string pid = cv.get_correct_pid(tpg);
            string pathProg = cv.get_correct_path_prog(tpg);
            string sout = "";

            if (!string.IsNullOrEmpty(pid) && Process.GetProcesses().Any(x => x.Id == Convert.ToInt32(pid)))
            {
                sout += "Process " + friendly_name + " is still running" + Environment.NewLine;
                return sout;
            }

            ProcessStartInfo psi = new ProcessStartInfo();
            if(tpg == typeProg.editor)
            {
                psi.WindowStyle = ProcessWindowStyle.Normal;
            }
            else
            {
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
            }
            psi.FileName = pathProg;
            psi.Arguments = "";
            foreach (string arg in args)
            {
                psi.Arguments += " \"" + arg + "\"";
            }
            
            var proc = Process.Start(psi);
            System.Threading.Thread.Sleep(500);


            if (tpg == typeProg.apache || tpg == typeProg.mariadb)
            {
                try
                {
                    if (checkRunningProc(proc.Id.ToString()))
                    {
                        //OK
                        sout += "Starting " + friendly_name + " with id " + proc.Id + Environment.NewLine;
                        cv.updatePID(tpg, typeStartorKill.start, proc.Id);
                    }
                    else
                    {
                        //string stdoutx = proc.StandardOutput.ReadToEnd();
                        string stderrx = proc.StandardError.ReadToEnd();

                        if (!string.IsNullOrEmpty(stderrx))
                        {
                            sout += "Error starting " + friendly_name + Environment.NewLine;
                            sout += "Error message " + stderrx;
                        }
                    }
                }
                catch(Exception ex)
                {
                    sout += "Error starting " + friendly_name + Environment.NewLine;
                    sout += ex.ToString();
                }
            }
            
            return sout;
        }

        public static string startProc_and_wait_output(string path_exe, string args, bool bhide = false, string working_dir = null)
        {
            string _outstring = "";
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            if(!string.IsNullOrEmpty(working_dir))
            {
                process.StartInfo.WorkingDirectory = working_dir;
            }
            process.StartInfo.Arguments = "/c \"" + path_exe + " " + args + "\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = bhide;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            //* Read the output (or the error)
            _outstring = process.StandardOutput.ReadToEnd();
            //Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            //Console.WriteLine(err);
            process.WaitForExit();
            return _outstring;
        }

        public static Tuple<string, string> startProc_and_wait_output2(string path_exe, string args, bool bhide = false, string working_dir = null, string enviromentPath = null, Dictionary<string,string> otherenvvars = null)
        {
            string _outstring = "";
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";
            if (!string.IsNullOrEmpty(working_dir))
            {
                process.StartInfo.WorkingDirectory = working_dir;
            }
            process.StartInfo.Arguments = "/c \"" + path_exe + " " + args + "\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = bhide;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            
            if(!string.IsNullOrEmpty(enviromentPath))
            {
                process.StartInfo.EnvironmentVariables["PATH"] = enviromentPath;
            }

            if(otherenvvars != null)
            {
                foreach(var arg in otherenvvars)
                {
                    process.StartInfo.EnvironmentVariables[arg.Key] = arg.Value;
                }
            }

                
            process.Start();
            //* Read the output (or the error)
            _outstring = process.StandardOutput.ReadToEnd();
            //Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            //Console.WriteLine(err);
            process.WaitForExit();
            return new Tuple<string, string>(_outstring, err);
        }

        public static void startProc_as_admin(string path_exe, string args)
        {
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Environment.CurrentDirectory;
            //proc.FileName = path_exe;
            proc.FileName = "notepad.exe"; // qui devo per forza usare notepad perchè notepad++ o altro potrebbe non funzionare
            proc.Verb = "runas";
            proc.Arguments = args;

            try
            {
                Process.Start(proc);
            }
            catch(Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.ToString());
                // The user refused the elevation.
                // Do nothing and return directly ...
                return;
            }
            //System.Windows.Forms.Application.Exit();  // Quit itself
        }


        public static int getProcessId_from_port(int _port)
        {
            using (Process Proc = new Process())
            {
                ProcessStartInfo StartInfo = new ProcessStartInfo();
                StartInfo.FileName = "netstat.exe";
                StartInfo.Arguments = "-a -n -o";
                StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                StartInfo.UseShellExecute = false;
                StartInfo.RedirectStandardInput = true;
                StartInfo.RedirectStandardOutput = true;
                StartInfo.RedirectStandardError = true;
                StartInfo.CreateNoWindow = true;

                Proc.StartInfo = StartInfo;
                Proc.Start();

                StreamReader StandardOutput = Proc.StandardOutput;
                StreamReader StandardError = Proc.StandardError;

                string NetStatContent = StandardOutput.ReadToEnd() + StandardError.ReadToEnd();
                string NetStatExitStatus = Proc.ExitCode.ToString();

                if (NetStatExitStatus != "0")
                {
                    Console.WriteLine("NetStat command failed.   This may require elevated permissions.");
                }

                string[] NetStatRows = Regex.Split(NetStatContent, "\r\n");

                foreach (string NetStatRow in NetStatRows)
                {
                    string[] Tokens = Regex.Split(NetStatRow, "\\s+");
                    if (Tokens.Length > 4 && (Tokens[1].Equals("UDP") || Tokens[1].Equals("TCP")))
                    {
                        string IpAddress = Regex.Replace(Tokens[2], @"\[(.*?)\]", "1.1.1.1");
                        try
                        {
                            int port = Convert.ToInt32(IpAddress.Split(':')[1]);
                            string processname = Tokens[1] == "UDP" ? GetProcessName(Convert.ToInt16(Tokens[4])) : GetProcessName(Convert.ToInt16(Tokens[5]));
                            int ProcessId = Tokens[1] == "UDP" ? Convert.ToInt16(Tokens[4]) : Convert.ToInt16(Tokens[5]);
                            string Protocol = IpAddress.Contains("1.1.1.1") ? String.Format("{0}v6", Tokens[1]) : String.Format("{0}v4", Tokens[1]);
                            if(_port == port)
                            {
                                return ProcessId;
                            }
                            /*
                            ProcessPorts.Add(new ProcessPort(
                                Tokens[1] == "UDP" ? GetProcessName(Convert.ToInt16(Tokens[4])) : GetProcessName(Convert.ToInt16(Tokens[5])),
                                Tokens[1] == "UDP" ? Convert.ToInt16(Tokens[4]) : Convert.ToInt16(Tokens[5]),
                                IpAddress.Contains("1.1.1.1") ? String.Format("{0}v6", Tokens[1]) : String.Format("{0}v4", Tokens[1]),
                                Convert.ToInt32(IpAddress.Split(':')[1])
                            ));*/
                        }
                        catch
                        {
                            //Console.WriteLine("Could not convert the following NetStat row to a Process to Port mapping.");
                            //Console.WriteLine(NetStatRow);
                        }
                    }
                    else
                    {
                        if (!NetStatRow.Trim().StartsWith("Proto") && !NetStatRow.Trim().StartsWith("Active") && !String.IsNullOrWhiteSpace(NetStatRow))
                        {
                            //Console.WriteLine("Unrecognized NetStat row to a Process to Port mapping.");
                            //Console.WriteLine(NetStatRow);
                        }
                    }
                }
            }

            return 0;
        }

        private static string GetProcessName(int ProcessId)
        {
            string procName = "UNKNOWN";

            try
            {
                procName = Process.GetProcessById(ProcessId).ProcessName;
            }
            catch { }

            return procName;
        }

        public static bool checkRunningProc(string pid)
        {
            if (string.IsNullOrEmpty(pid))
            {
                return false;
            }

            Process proc = ZampGUILib.GetProcByID(pid);
            if (proc != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static string getNameProc_fromPID(string pid)
        {
            if(string.IsNullOrEmpty(pid))
            {
                return "";
            }
            else
            {
                Process proc = ZampGUILib.GetProcByID(pid);
                return proc.ProcessName;
            }
            //return "";
        }

        public static string getStatusProc(ConfigVar cv, typeProg tpg)
        {
            string friendly_name = cv.get_friendly_name(tpg);
            string pid = cv.get_correct_pid(tpg);

            if (string.IsNullOrEmpty(pid))
            {
                return friendly_name + " not running" + Environment.NewLine;
            }

            Process proc = ZampGUILib.GetProcByID(pid);
            string sout = "";
            if (proc != null)
            {
                sout += string.Format(friendly_name + " {0}, Id: {1}", proc.ProcessName, proc.Id) + Environment.NewLine;
            }
            else
            {
                sout += friendly_name + " not running" + Environment.NewLine;
            }
            return sout;
        }
        public static string killproc(ConfigVar cv, typeProg tpg)
        {
            string friendly_name = cv.get_friendly_name(tpg);
            string pid = cv.get_correct_pid(tpg);
            string sout = "";


            if(string.IsNullOrEmpty(pid))
            {
                sout += friendly_name + " not running" + Environment.NewLine;
                return sout;
            }

            Process proc = null;
            try
            {
                proc = Process.GetProcessById(Convert.ToInt32(pid));
            }
            catch (Exception ex) { }

            if (proc != null)
            {
                sout += "killing proc " + friendly_name + " with id " + proc.Id + Environment.NewLine;
                proc.Kill();
            }
            else
            {
                sout += friendly_name + " not running" + Environment.NewLine;
            }
            cv.updatePID(tpg, typeStartorKill.kill, Convert.ToInt32(pid));
            return sout;
        }

        public static Process GetProcByID(string id)
        {
            Process[] processlist = Process.GetProcesses();
            return processlist.FirstOrDefault(pr => pr.Id == Convert.ToInt32(id));
        }

        public static string getJsonPath()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path_json_config = Path.Combine(assemblyFolder, "config.json");
            return path_json_config;
        }

        public static JObject getJson_Env()
        {
            string path_json_config = getJsonPath();
            string temp = File.ReadAllText(path_json_config);
            JObject o1 = JObject.Parse(temp);
            return o1;
        }
        public static void setJson_Env(JObject jsonObj)
        {
            string path_json_config = getJsonPath();
            string newJsonResult = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj,
                               Newtonsoft.Json.Formatting.Indented);
            if(System.IO.File.Exists(path_json_config))
            {
                System.IO.File.Delete(path_json_config);
            }
            File.WriteAllText(path_json_config, newJsonResult);
        }

        public static string replace_ignorecase(string text, string oldtext, string newtext)
        {
            return Regex.Replace(text, oldtext, newtext, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            //text.Replace(oldtext, newtext);
        }
        

        public static List<string> ExecuteBatchFile(string batchFileName, string[] argumentsToBatchFile)
        {
            string argumentsString = string.Empty;
            List<string> l_res = new List<string>();

            if (argumentsToBatchFile != null)
            {
                for (int count = 0; count < argumentsToBatchFile.Length; count++)
                {
                    argumentsString += " \"" + argumentsToBatchFile[count] + "\"";
                }
            }


            ProcessStartInfo ProcessInfo = new ProcessStartInfo(batchFileName, argumentsString);
            ProcessInfo.CreateNoWindow = true;
            ProcessInfo.UseShellExecute = false;
            //ProcessInfo.WorkingDirectory = Application.StartupPath + "\\txtmanipulator";
            // *** Redirect the output ***
            ProcessInfo.RedirectStandardError = true;
            ProcessInfo.RedirectStandardOutput = true;

            Process process = Process.Start(ProcessInfo);
            process.WaitForExit();

            // *** Read the streams ***
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            int ExitCode = process.ExitCode;

            process.Close();

            l_res.Add(output);
            l_res.Add(error);
            l_res.Add(ExitCode.ToString());
            return l_res;
        }
        public static bool ExecuteBatchFile_dont_wait(string batchFileName, string[] argumentsToBatchFile, string AddToPath = "")
        {
            string argumentsString = string.Empty;
            try
            {
                //Add up all arguments as string with space separator between the arguments
                if (argumentsToBatchFile != null)
                {
                    for (int count = 0; count < argumentsToBatchFile.Length; count++)
                    {
                        argumentsString += " \"" + argumentsToBatchFile[count] + "\"";
                    }
                }


                //Create process start information
                ProcessStartInfo DBProcessStartInfo = new ProcessStartInfo(batchFileName, argumentsString);
                DBProcessStartInfo.UseShellExecute = false;
                DBProcessStartInfo.RedirectStandardOutput = false;
                DBProcessStartInfo.RedirectStandardError = false;

                if(string.IsNullOrEmpty(AddToPath))
                {
                    string PATH = Environment.GetEnvironmentVariable("PATH");
                    DBProcessStartInfo.EnvironmentVariables["PATH"] = AddToPath + ";" + PATH;
                }
                

                Process dbProcess = Process.Start(DBProcessStartInfo);
                return true;
            }
            // Catch the SQL exception and throw the customized exception made out of that
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                //throw new CustomizedException(ARCExceptionManager.ErrorCodeConstants.generalError, ex.Message);
                return false;
            }
        }


        public static string get_first_dir(string abs_main_path, string search)
        {
            string[] arrdir = Directory.GetDirectories(Path.Combine(abs_main_path, "Apps"));
            foreach(string s in arrdir)
            {
                string dir_name = Path.GetFileName(s);
                if(dir_name.Contains(search))
                {
                    return dir_name;
                }
            }
            throw new Exception(search + " folder not found");
        }
        public static string[] get_dirs(string abs_main_path, string search)
        {
            List<string> temp = new List<string>();
            string[] arrdir = Directory.GetDirectories(Path.Combine(abs_main_path, "Apps"));
            foreach (string s in arrdir)
            {
                string dir_name = Path.GetFileName(s);
                if (dir_name.Contains(search))
                {
                    temp.Add(dir_name);
                }
            }
            return temp.ToArray();
        }
        public static string get_first_file(string dir, string search)
        {
            string[] arr = Directory.GetFiles(dir);
            foreach (string s in arr)
            {
                string name = Path.GetFileName(s);
                if (s.Contains(search))
                {
                    return s;
                }
            }
            return "";
        }


        public static string getNameCurrent_user()
        {
            WindowsIdentity id = WindowsIdentity.GetCurrent();
            return id.Name;
            //WindowsPrincipal principal = new WindowsPrincipal(id);
            //return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static List<string> getListSite(ConfigVar cv)
        {
            List<string> _list = new List<string>();
            string arrtest = ZampGUILib.startProc_and_wait_output(cv.Apache_exe, "-S", true);
            string pattern = @"\d*\snamevhost\s(\w+:{0,1}\w*@)?(\S+)(:[0-9]+)?(\/|\/([\w#!:.?+=&%@!\-\/]))?";
            Regex rgx = new Regex(pattern);
            string sentence = arrtest;

            foreach (Match match in rgx.Matches(sentence))
            {
                string[] arr = match.Value.Split(' ');
                if (arr[0] == "80")
                {
                    _list.Add("http://" + arr[2].Trim());
                }
                else if (arr[0] == "443")
                {
                    _list.Add("https://" + arr[2].Trim());
                }
                else
                {
                    _list.Add("http://" + arr[2].Trim() + ":" + arr[0]);
                }

                //lista.Add(arr[1].Trim());

                //Console.WriteLine("Found '{0}' at position {1}", match.Value, match.Index);
            }
            _list = _list.OrderBy(s => s).ToList();
            return _list;
        }
        public static bool checkstatusProc_byName(string nameproc)
        {
            System.Threading.Thread.Sleep(1000);
            Process[] proc = Process.GetProcessesByName(nameproc);
            if (proc.Count() > 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static string killproc_byName(string nameproc)
        {
            string sout = "";
            System.Threading.Thread.Sleep(1000);
            Process[] proc = Process.GetProcessesByName(nameproc);

            if (proc.Count() > 0)
            {
                foreach (var p in proc)
                {
                    sout += "killing proc " + p.ProcessName + " with id " + p.Id + Environment.NewLine;
                    p.Kill();
                }
            }
            else
            {
                sout += nameproc + " not running" + Environment.NewLine;
            }
            return sout;
        }
        public static string getApplicationFolder()
        {
            string assemblyFolder = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string root_folder = System.IO.Directory.GetParent(assemblyFolder).Parent.FullName;
            return root_folder;
        }
        public static string getRootFolder(System.Windows.Forms.Form f = null)
        {
            string root_folder = getApplicationFolder();
            string YN_DEBUG = getval_from_appsetting("YN_DEBUG");
            string temp_folder = getval_from_appsetting("temp_folder");
            if (YN_DEBUG.Equals("Y"))
            {
                if (!System.IO.Directory.Exists(temp_folder))
                {
                    ZampGUILib.printMsg_and_exit(temp_folder + " does not exists", true, f);
                }
                root_folder = temp_folder;
            }
            return root_folder;
        }
        #region old

        private string startProc_old(string nameproc, string pathProg)
        {
            string sout = "";
            Process[] proc = Process.GetProcessesByName(nameproc);
            if (proc.Count() == 0)
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = pathProg;
                //psi.Arguments = "/C start notepad.exe";
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                var proc1 = Process.Start(psi);
                sout += "Starting " + proc1.Id + " " + nameproc + Environment.NewLine;
            }
            else
            {
                sout += "Process " + nameproc + " is already running" + Environment.NewLine;
            }
            return sout;
        }
        
        public static bool ExecuteBatchFile_OLD(string batchFileName, string[] argumentsToBatchFile)
        {
            string argumentsString = string.Empty;
            try
            {
                //Add up all arguments as string with space separator between the arguments
                if (argumentsToBatchFile != null)
                {
                    for (int count = 0; count < argumentsToBatchFile.Length; count++)
                    {
                        argumentsString += " ";
                        argumentsString += argumentsToBatchFile[count];
                        //argumentsString += "\"";
                    }
                }

                //Create process start information
                System.Diagnostics.ProcessStartInfo DBProcessStartInfo = new System.Diagnostics.ProcessStartInfo(batchFileName, argumentsString);

                //Redirect the output to standard window
                DBProcessStartInfo.RedirectStandardOutput = true;

                //The output display window need not be falshed onto the front.
                DBProcessStartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                DBProcessStartInfo.UseShellExecute = false;

                //Create the process and run it
                System.Diagnostics.Process dbProcess;
                dbProcess = System.Diagnostics.Process.Start(DBProcessStartInfo);

                //Catch the output text from the console so that if error happens, the output text can be logged.
                System.IO.StreamReader standardOutput = dbProcess.StandardOutput;

                /* Wait as long as the DB Backup or Restore or Repair is going on. 
                Ping once in every 2 seconds to check whether process is completed. */
                while (!dbProcess.HasExited)
                    dbProcess.WaitForExit(2000);

                if (dbProcess.HasExited)
                {
                    string consoleOutputText = standardOutput.ReadToEnd();
                    //TODO - log consoleOutputText to the log file.

                }

                return true;
            }
            // Catch the SQL exception and throw the customized exception made out of that
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
                //throw new CustomizedException(ARCExceptionManager.ErrorCodeConstants.generalError, ex.Message);
                return false;
            }
        }
        #endregion
    }
}
