using System;
using System.Data.Entity;
using System.Linq;
using System.Collections.Generic;

namespace MESAPI.Models
{
    public class MachineDetailModel
    {
        public string MACHINE_NAME { get; set; }
        public string EQUIPMENT_ID { get; set; }
        public string EQUIPMENT_CODE { get; set; }
        public string EQUIPMENT_TYPE { get; set; }
        public string EQUIPMENT_STATUS { get; set; }
        public string EQUIPMENT_STATUS_CODE { get; set; }
    }
    public class MachineListbyLineNameModel
    {
        public List<MachineDetailModel> MACHINE_LIST { get; set; }
    }
    public class MachineListbyProdAreaModel
    {
        public string LINE_NAME { get; set; }
        public List<MachineDetailModel> MACHINE_LIST { get; set; }
    }
    public class MachineInfoModel
    {
        public string EQUIPMENT_ID { get; set; }
        public string EQUIPMENT_TYPE { get; set; }
        public string MACHINE_NAME { get; set; }
        public string STATION_NAME { get; set; }
        public string GROUP_NAME { get; set; }
        public string IMAGE_URL { get; set; }
        public string LINE_NAME { get; set; }
        public string EQUIPMENT_STATUS { get; set; }
        public string EQUIPMENT_STATUS_CODE { get; set; }
    }
    public class MachineTBSModel
    {
        public string DATETIME { get; set; }
        public decimal TBS { get; set; }
    }
    public class MachineUtiModel
    {
        public string DATETIME { get; set; }
        public decimal UTILIZATION_RATE { get; set; }
    }
    public class MachinePlantInfoModel
    {
        public decimal UTILIZATION_RATE_TODAY { get; set; }
        public decimal UTILIZATION_RATE_YESTERDAY { get; set; }
        public decimal AVERAGE_TBS_TODAY { get; set; }
        public decimal AVERAGE_TBS_YESTERDAY { get; set; }
        public List<MachinePlantInfoUtiDetailModel> UTILIZATION_LINE { get; set; }
        public List<MachinePlantInfoTbsDetailModel> AVERAGE_TBS_LINE { get; set; }
    }
    public class MachinePlantInfoUtiDetailModel
    {
        public string LINE_NAME { get; set; }
        public string UTILIZATION { get; set; }
    }
    public class MachinePlantInfoTbsDetailModel
    {
        public string LINE_NAME { get; set; }
        public string TBS { get; set; }
    }
    public class MachineLineInfoModel
    {
        public string CURRENT_MO { get; set; }
        public string CURRENT_MODEL { get; set; }
        public decimal UTILIZATION_RATE_TODAY { get; set; }
        public decimal UTILIZATION_RATE_YESTERDAY { get; set; }
        public List<MachineLineInfoUtiDetailModel> UTILIZATION_MACHINE { get; set; }
        public decimal AVERAGE_TBS { get; set; }
        public List<MachineLineInfoTbsDetailModel> AVERAGE_TBS_MACHINE { get; set; }
        public string AUTOMATION_DEGREE { get; set; }
        public int DL_COUNT { get; set; }
        public int EQUIPMENT_COUNT { get; set; }
        public int CALCULATE_DAY { get; set; }
        public decimal FPYR { get; set; }
        public int UPH { get; set; }
        public string OI_RATE { get; set; }
        public List<MachineLineInfoLineBalanceDetailModel> LINE_BALANCE { get; set; }
        public string STD_ARCHIVE_PERCENTAGE_MAIN_LINE { get; set; }
        public string STD_ARCHIVE_PERCENTAGE_FINAL_LINE { get; set; }
        public List<MachineLineInfoSTDACHDetailModel> STD_ARCHIVE_DATA { get; set; }

    }
    public class MachineLineInfoUtiDetailModel
    {
        public string EQUIPMENT_CODE { get; set; }
        public string MACHINE_NAME { get; set; }
        public decimal UTILIZATION { get; set; }
    }
    public class MachineLineInfoTbsDetailModel
    {
        public string EQUIPMENT_CODE { get; set; }
        public string MACHINE_NAME { get; set; }
        public decimal TBS { get; set; }
    }
    public class MachineLineInfoLineBalanceDetailModel
    {
        public string MACHINE_NAME { get; set; }
        public decimal CYCLE_TIME { get; set; }
    }
    public class MachineLineInfoSTDACHDetailModel
    {
        public string PROCESS_NAME { get; set; }
        public string QTY_TYPE { get; set; }
        public decimal SAFE_RATE { get; set; }
        public decimal ALERT_RATE { get; set; }
        public decimal T0730_0830 { get; set; }
        public decimal T0830_0930 { get; set; }
        public decimal T0930_1030 { get; set; }
        public decimal T1030_1130 { get; set; }
        public decimal T1130_1230 { get; set; }
        public decimal T1230_1330 { get; set; }
        public decimal T1330_1430 { get; set; }
        public decimal T1430_1530 { get; set; }
        public decimal T1530_1700 { get; set; }
        public decimal T2000_2100 { get; set; }
        public decimal T2100_2200 { get; set; }
        public decimal T2200_2300 { get; set; }
        public decimal T2300_2400 { get; set; }
        public decimal T0000_0100 { get; set; }
        public decimal T0130_0200 { get; set; }
        public decimal T0200_0300 { get; set; }
        public decimal T0300_0400 { get; set; }
        public decimal T0400_0500 { get; set; }
    }
    public class MachinePostDataModel
    {
        public string EQUIPMENT_ID { get; set; }
        public string INTERFACE_ID { get; set; }
        public string STATUS { get; set; }
        public string STATUS_CODE { get; set; }
        public string PASS_QTY { get; set; }
        public string FAIL_QTY { get; set; }
        public string ERROR_CNT { get; set; }
        public string ERROR_TIMES { get; set; }
        public string CYCLE_TIME { get; set; }
        public string RUNNING_TIME { get; set; }
        public string WAITING_TIME { get; set; }
        public string SELF_CHECK { get; set; }
        public string INPUT_QTY { get; set; }
        public string BARCODE { get; set; }
        public string MODEL { get; set; }
        public string COLLECT_DATE { get; set; }
        public List<MachinePostDataDetailModel> PARAM_LIST { get; set; }
    }
    public class MachinePostDataDetailModel
    {
        public string PARAM_CODE { get; set; }
        public string PARAM_VALUE { get; set; }
        public string LOCATION { get; set; }
    }
}