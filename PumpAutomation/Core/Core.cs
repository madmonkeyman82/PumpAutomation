﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using PumpAutomation;
using PumpAutomation.PLC;


//using Modbus;
//using ModbusTcp;


namespace PumpAutomation
{
    public sealed class Core
    {
        #region Constructor / Deconstructor

        private static readonly Lazy<Core> lazy = new Lazy<Core>(() => new Core());
        public static Core Instance { get { return lazy.Value; } }

        //Constructor
        private Core()
        {
            SingletonLogger.AddToLog("Loading Core", LogType.Info, LogModule.CORE);
            
            LoadSettings();
            
            
            SingletonLogger.AddToLog("Core Loaded", LogType.Info, LogModule.CORE);
        }

        //Deconstructor
        ~Core()
        {

        }

        #endregion

        #region Private Variables
        //Private Variables

        //String`s
        private string _Status = "Startup";
 

        // Ethernet Variables
        private string _sPlcIPAddress;
        private int _iPlcPort;
        private int _iTimeout;

        // Thread`s
        private Thread _tThreadUpdatePlcVariablesSlowCycle;
       // private Thread _tThreadUpdatePlcVariablesFastCycle;
        private int _iTheadUpdateSlowDelay;
        private int _iTheadUpdateFastDelay;


        // bool`s
        private bool _bStopUpdatePlcThread = false;
        private bool _bIsPlcConnected = false;

   

        // Communication Modules
        // public Modbus modbusrtu = new Modbus();
        private Modbus _modbusControl;
        


        // Communication Modules
        // public Modbus moudbusrtu = new Modbus();
        //public Plc PlcModule = new Plc();

        // Logger Variable
        private Logger SingletonLogger = Logger.Instance;
        
 
        #endregion

        #region Public Variables
        //Public Variables

        public PlcVariables _PlcVariables = new PlcVariables();

        #endregion

        #region Private Methods

        // Private Methods


        #region  Plc data acquisition

        #region  Plc data acquisition thread

        private void StartUpdatePlcVarsThread()
        {
            _tThreadUpdatePlcVariablesSlowCycle = new Thread(new ThreadStart(this.ThreadUpdatePlcVars));
            _tThreadUpdatePlcVariablesSlowCycle.IsBackground = true;
            _tThreadUpdatePlcVariablesSlowCycle.Name = "PLC UPDATE THREAD";
            _tThreadUpdatePlcVariablesSlowCycle.Start();
        }

        private void StopUpdatePlcVarsThread()
        {
            _tThreadUpdatePlcVariablesSlowCycle.Abort();
            while (_tThreadUpdatePlcVariablesSlowCycle.IsAlive)
            {
                Thread.Sleep(10);
            }
            _tThreadUpdatePlcVariablesSlowCycle = null;
        }


        //Data colecting fra plc thread
        public void ThreadUpdatePlcVars()
        {
            SingletonLogger.AddToLog("Plc Vars Update Thread has Started", LogType.Info, LogModule.CORE);

            while (!_bStopUpdatePlcThread && _modbusControl != null)
            {
                while (_bIsPlcConnected)
                {

                    ReadAllCoils();
                    ReadAllVMemories();
                    //ReadPlcTime();
                    test();
                    Thread.Sleep(_iTheadUpdateSlowDelay);
                }
            }
            SingletonLogger.AddToLog("Plc Vars Update Thread has Stopped", LogType.Warning, LogModule.CORE);
        }

        /// <summary>
        /// Main update thread!!!
        /// </summary>
        private void ReadAllCoils()
        {
                _PlcVariables._MBPump1Start = _modbusControl.CoilsData[(int)CPlcVariableDoMore.MBPump1Start];             
                _PlcVariables._MBPump1On = _modbusControl.CoilsData[(int)CPlcVariableDoMore.MBPump1On]; 
                _PlcVariables._MBSimulationMode = _modbusControl.CoilsData[(int)CPlcVariableDoMore.MBSimulationMode];
                _PlcVariables._MBWatchDog = _modbusControl.CoilsData[(int)CPlcVariableDoMore.MBWatchDog];
                _PlcVariables._MBIsAlive = _modbusControl.CoilsData[(int)CPlcVariableDoMore.MBIsAlive];
        }

        private void ReadAllVMemories()
        {
                _PlcVariables.MBFlow1 = _modbusControl.RegisterData[(int)VPlcVariable.MBFlow1];
                _PlcVariables.MBPressure1 = _modbusControl.RegisterData[(int)VPlcVariable.MBPressure1];
                _PlcVariables.MB_T_Filter1 = _modbusControl.RegisterData[(int)VPlcVariable.MB_T_Filter1];
                _PlcVariables.MBConSens4 = _modbusControl.RegisterData[(int)VPlcVariable.MBConSens4];
                _PlcVariables.MBConSens6 = _modbusControl.RegisterData[(int)VPlcVariable.MBConSens6];
                _PlcVariables.MBConSens14 = _modbusControl.RegisterData[(int)VPlcVariable.MBConSens14];
                _PlcVariables.MBConSens21 = _modbusControl.RegisterData[(int)VPlcVariable.MBConSens21];
                _PlcVariables.MBConSensStaus = _modbusControl.RegisterData[(int)VPlcVariable.MBConSensStaus];
                _PlcVariables.MBConSensmA = _modbusControl.RegisterData[(int)VPlcVariable.MBConSensmA];
                _PlcVariables.MBFlow2 = _modbusControl.RegisterData[(int)VPlcVariable.MBFlow2];
                _PlcVariables.MBPressure2 = _modbusControl.RegisterData[(int)VPlcVariable.MBPressure2];
                _PlcVariables.MBWaterContent = _modbusControl.RegisterData[(int)VPlcVariable.MBWaterContent];
                _PlcVariables.MBOilTemp = _modbusControl.RegisterData[(int)VPlcVariable.MBOilTemp];
                _PlcVariables.MB_T_Filter2 = _modbusControl.RegisterData[(int)VPlcVariable.MB_TFilter2];
                _PlcVariables.MBRFilter = _modbusControl.RegisterData[(int)VPlcVariable.MBRFilter];          
        }

        private void ReadPlcTime()
        {
            _PlcVariables.PlcTime = "PLC Time " +
                                    _modbusControl.RegisterData[(int)VPlcTime.PlcHour].ToString() + ":" +
                                    _modbusControl.RegisterData[(int)VPlcTime.PLCMin].ToString() + ":" +
                                    _modbusControl.RegisterData[(int)VPlcTime.PLCSecond].ToString();
        }

        #endregion



        #endregion


        private void LoadSettings()
        {
            LoadVariables();

            _modbusControl = new Modbus();
            
            LoadPLCVariables();
        }

        private void LoadVariables()
        {
            _iTheadUpdateFastDelay = Properties.Settings.Default.PlcUpdatetimeFastCylce;
            _iTheadUpdateSlowDelay = Properties.Settings.Default.PlcUpdatetimeSlowCylce;
        }


        private void LoadPLCVariables()
        {

        }

        #endregion

        #region Public Methods
        // Public Methods

            #region PLC method`s

            public bool ConnectToPlc()
            {
                    SingletonLogger.AddToLog("Connecting to PLC using Ethernet ", LogType.Info, LogModule.PLC);
                    try
                    {
                        if ( _modbusControl.Connect())
                        {
                                _bIsPlcConnected = true;
                                _bStopUpdatePlcThread = false;
                                SingletonLogger.AddToLog("Connecting to PLC = " , LogType.Info, LogModule.COM);
                                StartUpdatePlcVarsThread(); // Update thread    
                                return true;
                        }
                        else
                        {
                            _bIsPlcConnected = false;
                            return false;
                        }
                    }
                    catch (Exception e)
                    {
                        //SingletonLogger.AddToLog(e.ToString(), LogType.Error, LogModule.PLC);

                        return false;
                    }          
            }

            public void DisconnectFromPlc()
            {
                try
                {
                        SingletonLogger.AddToLog("Disconnecting from PLC", LogType.Info, LogModule.PLC);
                        _bStopUpdatePlcThread = true;
                        _bIsPlcConnected = false;
                        SingletonLogger.AddToLog("Disconnecting from PLC = ", LogType.Info, LogModule.COM);
                        Thread.Sleep(10);
                        StopUpdatePlcVarsThread();
                        Thread.Sleep(10);
                        _modbusControl.Disconnect();
                        
                }
                catch (Exception e)
                {
                    SingletonLogger.AddToLog(e.ToString(), LogType.Error, LogModule.PLC);       
                }
            }

            public void test()
            {
              //  _modbusControl.test(); 
                //int PrefSpeed = _modbusControl.PreformanceTimeMs;
            }

            #region PLC Read

            #region Coils


            /// <summary>
            /// Read Bool status of single coil by ushort address
            /// </summary>
            /// <param name="adresse"></param>
            /// <returns></returns>
            public bool GetCoilStatus(ushort address)
            {
               // return _modbusControl.GetCoilStatus(address);
                return false;
            }

            /// <summary>
            /// Read Bool status of range of coil`s 
            /// </summary>
            /// <param name="adresse">This is the start adress of the coils</param>
            /// <param name="Coils">Bool Array to hold the coils </param>
            /// <returns></returns>
            public bool GetCoils(ushort address , ref bool[] Coils)
            {
                /*
                if (_modbusControl.GetCoils(address, ref Coils))
                {
                    return true;
                }
                 */ 
                return false;
            }

            #endregion // Coils

            #region Holding registers


            /// <summary>
            /// Read Word 16bit from ushort address
            /// </summary>
            /// <param name="Vtype"></param>
            /// <returns></returns>
            public short ReadWordValue(PumpAutomation.PLC.VPlcVariable Vtype)
            {
                    short[] VMemValue = new short[1];
                    
                    ushort addess = (PlcCovnertions.GetVariableAddresse(Vtype, false));

                   // VMemValue[0] = _modbusControl.ReadWordValue(addess);

                    return VMemValue[0];
            }
            
            public short ReadWordValue(PumpAutomation.PLC.VPlcTime Vtype)
            {
                short[] VMemValue = new short[1];
         
                ushort addess = (PlcCovnertions.GetVariableAddresse(Vtype));

              //  VMemValue[0] = _modbusControl.ReadWordValue(addess);

                return VMemValue[0];
            }

            /// <summary>
            /// Read Word 16bit from ushort address
            /// </summary>
            /// <param name="address"> Start address</param>
            /// <param name="Registers">How Many registers to read</param>
            /// <returns></returns>
            public bool ReadWordValueS(ushort address, ushort quantity , ref short[] Registers)
            {

                bool _bStatus = false;//_modbusControl.ReadWordValueS(address, quantity, ref Registers);

                if (_bStatus)
                {
                    return true;
                }
                else
                {
                   // SingletonLogger.AddToLog(_modbusControl.modbusControlSerial.GetLastErrorString() + _Protocol, LogType.Error, LogModule.COM);
                }
                return false;
            }

            #endregion // Holding registers

            #endregion // PLC Read

            #region PLC Write

            #region Coils

            /// <summary>
            /// Write to DoMore MC coils
            /// </summary>
            /// <param name="Ctype"></param>
            /// <param name="NewCoilStatus"></param>
            /// <returns></returns>
            public bool WriteBoolValue(PumpAutomation.PLC.CPlcVariableDoMore Ctype, bool NewCoilStatus)
            {
                bool WriteOk = false;
                WriteOk = WriteToCoil(Ctype, NewCoilStatus);
                return WriteOk;
            }

            /// <summary>
            /// Write to Coil register by Name eks: "C54" address with value either true / false
            /// </summary>
            /// <param name="ctype"></param>
            /// <param name="NewCoilStatus"></param>
            /// <returns></returns>
            public bool WriteToCoil(PumpAutomation.PLC.CPlcVariableDoMore ctype, bool NewCoilStatus)
            {
                return WriteToCoil(PlcCovnertions.GetVariableAddresse(ctype, false), NewCoilStatus);
            }

            /// <summary>
            /// Write to Coil register by ushort address With value either true / false
            /// </summary>
            /// <param name="adresse"></param>
            /// <param name="NewCoilStatus"></param>
            /// <returns></returns>
            public bool WriteToCoil(ushort address, bool NewCoilStatus)
            {
                bool _Status = false;

                _Status = false; //_modbusControl.WriteToCoil(address, NewCoilStatus);
               // _Status = modbusControlSerial.WriteSingleCoilFC5(1, address, NewCoilStatus);

                if (_Status)
                {
                    return true;
                }
                else
                {
                    SingletonLogger.AddToLog("Error in plc --> com ", LogType.Error, LogModule.PLC);

                }

                return false; ;
            }

             #endregion // Coils

            #region Holding Registers

            /// <summary>
            /// Write to Holding register by Name 
            /// </summary>
            /// <param name="ctype"></param>
            /// <param name="NewCoilStatus"></param>
            /// <returns></returns>
            public bool WriteToRegister(PumpAutomation.PLC.VPlcVariable Vtype, short SetValue)
            {
                return WriteToRegister(PlcCovnertions.GetVariableAddresse(Vtype, false), SetValue);
            }

            /// <summary>
            /// Write to Holding register ex:MHR1, MHR2 by short address with value either 16bit value
            /// </summary>
            /// <param name="adresse"></param>
            /// <param name="SetValue"></param>
            /// <returns></returns>
            public bool WriteToRegister(ushort address, short SetValue)
            {
                bool _Status = false;

              //  _Status = _modbusControl.WriteToRegister(address, SetValue);


                if (_Status)
                {
                    return true;
                }
                else
                {
                    SingletonLogger.AddToLog("Error in plc --> com ", LogType.Error, LogModule.PLC);
                }

                return false;
            }

    #endregion

            #endregion

    #endregion

    #endregion

        #region Get / Set

        public string Status { get { return _Status; } }

        public int ModbusPreformance { get { return _modbusControl.PreformanceTimeMs; } }

        public BitArray ModbusCoilArray { get { return _modbusControl.CoilsData; } }

        public bool IsPlcConnected 
        {
            get
            {
                if (_bIsPlcConnected && _PlcVariables._MBWatchDog)
                 {
                   return true;
                 }
                else
                {
                    return false;
                }
             } 
        }

        public bool IsSimulationMode
        {
            get { return _PlcVariables._MBSimulationMode; }
            set
            {
                if (_bIsPlcConnected)
                {
                    if (_PlcVariables._MBSimulationMode)
                    {
                        WriteBoolValue(CPlcVariableDoMore.MBSimulationMode, false);
                    }
                    else
                    {
                        WriteBoolValue(CPlcVariableDoMore.MBSimulationMode, true);
                    }
                
                }
            }
        }

        #endregion

        }

        
    

    }
