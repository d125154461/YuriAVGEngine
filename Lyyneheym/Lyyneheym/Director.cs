﻿//#define NOTIME
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Yuri.Utils;
using Yuri.PlatformCore;
using Yuri.ILPackage;

namespace Yuri
{
    /// <summary>
    /// <para>导演类：管理整个游戏生命周期的类</para>
    /// <para>她是一个单例类，只有唯一实例</para>
    /// </summary>
    public class Director
    {

        #region debug用
        public MySprite testBitmapImage(string filename)
        {
            return this.ResMana.GetBackground(filename, new System.Windows.Int32Rect(-1, 0, 0, 0));
        }

        public MySprite testCharaStand(string filename)
        {
            return this.ResMana.GetCharacterStand(filename, new System.Windows.Int32Rect(-1, 0, 0, 0));
        }


        public void testBGM(string sourceName)
        {
            Musician m = Musician.GetInstance();
            var r = this.ResMana.GetBGM(sourceName);
            m.PlayBGM(sourceName, r.Key, r.Value, 1000);

        }

        public void testVocal(string vocalName)
        {
            Musician m = Musician.GetInstance();
            var r = this.ResMana.GetVocal(vocalName);
            m.PlayVocal(r.Key, r.Value, 1000);
        }
        #endregion

        #region 初次进入时的初始化相关函数
        /// <summary>
        /// 初始化游戏设置
        /// </summary>
        private void InitConfig()
        {
            // 读入游戏设定INI
            
            // 把设定写到GlobalDataContainer

            // 应用设置到窗体

        }

        /// <summary>
        /// 初始化运行时环境，并指定脚本的入口
        /// </summary>
        private void InitRuntime()
        {
            var mainScene = this.ResMana.GetScene(GlobalDataContainer.Script_Main);
            if (mainScene == null)
            {
                DebugUtils.ConsoleLine(String.Format("No Entry Point Scene: {0}, Program will exit.", GlobalDataContainer.Script_Main),
                    "Director", OutputStyle.Error);
                Environment.Exit(0);
            }
            foreach (var sc in this.ResMana.GetAllScene())
            {
                this.RunMana.Symbols.AddSymbolTable(sc);
            }
            this.RunMana.CallScene(mainScene);
        }
        #endregion

        #region 前端更新后台相关函数
        /// <summary>
        /// 提供由前端更新后台键盘按键信息的方法
        /// </summary>
        /// <param name="e">键盘事件</param>
        public void UpdateKeyboard(KeyEventArgs e)
        {
            this.updateRender.SetKeyboardState(e.Key, e.KeyStates);
            DebugUtils.ConsoleLine(String.Format("Keyboard event: {0} <- {1}", e.Key.ToString(), e.KeyStates.ToString()),
                "Director", OutputStyle.Normal);
        }

        /// <summary>
        /// 提供由前端更新后台鼠标按键信息的方法
        /// </summary>
        /// <param name="e">鼠标事件</param>
        public void UpdateMouse(MouseButtonEventArgs e)
        {
            this.updateRender.SetMouseButtonState(e.ChangedButton, e.ButtonState);
        }

        /// <summary>
        /// 提供由前端更新后台鼠标滚轮信息的方法
        /// </summary>
        /// <param name="delta">鼠标滚轮差分，上滚为正，下滚为负</param>
        public void UpdateMouseWheel(int delta)
        {
            this.updateRender.SetMouseWheelDelta(delta);
        }
        #endregion

        #region 辅助函数
        /// <summary>
        /// 向运行时环境发出中断
        /// </summary>
        /// <param name="ntr">中断</param>
        public void SubmitInterrupt(Interrupt ntr)
        {
            this.RunMana.CallStack.Submit(ntr);
        }

        /// <summary>
        /// 向运行时环境发出等待要求
        /// </summary>
        /// <param name="waitSpan">等待的时间间隔</param>
        public void SubmitWait(TimeSpan waitSpan)
        {
            this.RunMana.Delay("Director", DateTime.Now, waitSpan);
        }

        /// <summary>
        /// 从屏幕移除按钮，用于按钮自我消除
        /// </summary>
        /// <param name="id">按钮id</param>
        public void RemoveButton(int id)
        {
            this.updateRender.Deletebutton(id);
        }
        #endregion

        #region 消息循环
        /// <summary>
        /// 处理消息循环
        /// </summary>
        private void UpdateContext(object sender, EventArgs e)
        {
            // 取得调用堆栈顶部状态
            StackMachineState stackState = this.RunMana.GameState();
            switch (stackState)
            {
                case StackMachineState.Interpreting:
                case StackMachineState.FunctionCalling:
                    this.curState = GameState.Performing;
                    break;
                case StackMachineState.WaitUser:
                    this.curState = GameState.WaitForUserInput;
                    break;
                case StackMachineState.WaitAnimation:
                    this.curState = GameState.WaitAni;
                    break;
                case StackMachineState.Await:
                    this.curState = GameState.Waiting;
                    break;
                case StackMachineState.Interrupt:
                    this.curState = GameState.Interrupt;
                    break;
                case StackMachineState.NOP:
                    this.curState = GameState.Exit;
                    break;
            }
            // 根据调用堆栈顶部更新系统
            switch (this.curState)
            {
                // 等待状态
                case GameState.Waiting:
                    // 计算已经等待的时间（这里，不考虑并行处理）
                    if (DateTime.Now - this.RunMana.CallStack.ESP.timeStamp > this.RunMana.CallStack.ESP.delay)
                    {
                        this.RunMana.ExitCall();
                    }
                    break;
                // 等待动画
                case GameState.WaitAni:
                    if (SpriteAnimation.isAnyAnimation() == false)
                    {
                        this.RunMana.ExitCall();
                    }
                    break;
                // 等待用户操作
                case GameState.WaitForUserInput:
                    break;
                // 中断
                case GameState.Interrupt:
                    var interruptSa = this.RunMana.CallStack.ESP.IP;
                    var interruptExitPoint = this.RunMana.CallStack.ESP.aTag;
                    // 处理可选表达式计算
                    if (interruptSa != null)
                    {
                        var iterSa = interruptSa;
                        while (iterSa != null)
                        {
                            this.updateRender.Accept(interruptSa);
                            iterSa = iterSa.next;
                        }
                    }
                    // 退出中断
                    this.RunMana.ExitCall();
                    // 跳出所有用户等待
                    this.RunMana.ExitUserWait();
                    // 处理跳转
                    if (interruptExitPoint != null)
                    {
                        var curScene = this.ResMana.GetScene(this.RunMana.CallStack.EBP.bindingSceneName);
                        if (!curScene.labelDictionary.ContainsKey(interruptExitPoint))
                        {
                            DebugUtils.ConsoleLine(String.Format("Ignored Interrupt jump Instruction (target not exist): {0}", interruptExitPoint),
                                        "Director", OutputStyle.Error);
                            break;
                        }
                        this.RunMana.CallStack.EBP.IP = curScene.labelDictionary[interruptExitPoint];
                    }
                    break;
                // 演绎脚本
                case GameState.Performing:
                    // 取下一动作
                    var nextInstruct = this.RunMana.MoveNext();
                    // 处理影响调用堆栈的动作
                    if (nextInstruct.aType == SActionType.act_wait)
                    {
                        double waitMs = nextInstruct.argsDict.ContainsKey("time") ?
                                (double)this.RunMana.CalculatePolish(nextInstruct.argsDict["time"]) : 0;
                        this.RunMana.Delay(nextInstruct.saNodeName, DateTime.Now, TimeSpan.FromMilliseconds(waitMs));
                        break;
                    }
                    else if (nextInstruct.aType == SActionType.act_waitani)
                    {
                        this.RunMana.AnimateWait(nextInstruct.saNodeName);
                        break;
                    }
                    else if (nextInstruct.aType == SActionType.act_waituser)
                    {
                        this.RunMana.UserWait("Director", nextInstruct.saNodeName);
                        break;
                    }
                    else if (nextInstruct.aType == SActionType.act_jump)
                    {
                        var jumpToScene = nextInstruct.argsDict["filename"];
                        var jumpToTarget = nextInstruct.argsDict["target"];
                        // 场景内跳转
                        if (jumpToScene == "")
                        {
                            var currentScene = this.ResMana.GetScene(this.RunMana.CallStack.ESP.bindingSceneName);
                            if (!currentScene.labelDictionary.ContainsKey(jumpToTarget))
                            {
                                DebugUtils.ConsoleLine(String.Format("Ignored Jump Instruction (target not exist): {0}", jumpToTarget),
                                    "Director", OutputStyle.Error);
                                break;
                            }
                            this.RunMana.CallStack.ESP.IP = currentScene.labelDictionary[jumpToTarget];
                        }
                        // 跨场景跳转
                        else
                        {
                            var jumpScene = this.ResMana.GetScene(jumpToScene);
                            if (jumpScene == null)
                            {
                                DebugUtils.ConsoleLine(String.Format("Ignored Jump Instruction (scene not exist): {0}", jumpToScene),
                                    "Director", OutputStyle.Error);
                                break;
                            }
                            if (jumpToTarget != "" && !jumpScene.labelDictionary.ContainsKey(jumpToTarget))
                            {
                                DebugUtils.ConsoleLine(String.Format("Ignored Jump Instruction (target not exist): {0} -> {1}", jumpToScene, jumpToTarget),
                                    "Director", OutputStyle.Error);
                                break;
                            }
                            this.RunMana.ExitCall();
                            this.RunMana.CallScene(jumpScene, jumpToTarget == "" ? jumpScene.mainSa : jumpScene.labelDictionary[jumpToTarget]);
                        }
                        break;
                    }
                    else if (nextInstruct.aType == SActionType.act_call)
                    {
                        var callFunc = nextInstruct.argsDict["name"];
                        var signFunc = nextInstruct.argsDict["sign"];
                        if (signFunc != "" && (!signFunc.StartsWith("(") || !signFunc.EndsWith(")")))
                        {
                            DebugUtils.ConsoleLine(String.Format("Ignored Function calling (sign not valid): {0} -> {1}", callFunc, signFunc),
                                "Director", OutputStyle.Error);
                            break;
                        }
                        var sceneFuncContainer = this.ResMana.GetScene(this.RunMana.CallStack.ESP.bindingSceneName).funcContainer;
                        var sceneFuncList = from f in sceneFuncContainer where f.callname == callFunc select f;
                        if (sceneFuncList.Count() == 0)
                        {
                            DebugUtils.ConsoleLine(String.Format("Ignored Function calling (function not exist): {0}", callFunc),
                                "Director", OutputStyle.Error);
                            break;
                        }
                        var sceneFunc = sceneFuncList.First();
                        var signItem = signFunc.Replace("(", "").Replace(")", "").Split(new char[] {','}, StringSplitOptions.RemoveEmptyEntries);
                        if (sceneFunc.param.Count != signItem.Length)
                        {
                            DebugUtils.ConsoleLine(String.Format("Ignored Function calling (in {0}, require args num: {1}, but actual:{2})", callFunc, sceneFunc.param.Count, signItem.Length),
                                "Director", OutputStyle.Error);
                            break;
                        }
                        // 处理参数列表
                        List<object> argsVec = new List<object>();
                        foreach (var s in signItem)
                        {
                            string trimedPara = s.Trim();
                            object varref = null;
                            if (trimedPara.StartsWith("$") || trimedPara.StartsWith("&"))
                            {
                                varref = this.RunMana.Fetch(trimedPara);
                            }
                            else if (trimedPara.StartsWith("\"") && trimedPara.EndsWith("\""))
                            {
                                varref = (string)trimedPara;
                            }
                            else
                            {
                                varref = Convert.ToDouble(trimedPara);
                            }
                            argsVec.Add(varref);
                        }
                        this.RunMana.CallFunction(sceneFunc, argsVec);
                        break;
                    }
                    // 处理常规动作
                    this.updateRender.Accept(nextInstruct);
                    break;
                // 退出
                case GameState.Exit:
                    this.updateRender.Shutdown();
                    break;
            }
            // 处理IO
            this.updateRender.UpdateForMouseState();
            this.updateRender.UpdateForKeyboardState();
            // 处理并行调用
            this.updateRender.ParallelProcessor();
        }

        /// <summary>
        /// 当前游戏的状态
        /// </summary>
        private GameState curState;
        #endregion

        #region 导演类自身资源相关
        /// <summary>
        /// 设置主窗体引用
        /// </summary>
        /// <param name="mw">主窗体</param>
        public void SetMainWindow(MainWindow mw)
        {
            this.updateRender.SetMainWindow(this.mwReference = mw);
        }

        /// <summary>
        /// 在游戏结束时释放所有资源
        /// </summary>
        public void DisposeResource()
        {
            DebugUtils.ConsoleLine(String.Format("Begin dispose resource"), "Director", OutputStyle.Important);
            BassPlayer.GetInstance().Dispose();
            DebugUtils.ConsoleLine(String.Format("Finished dispose resource, program will shutdown"), "Director", OutputStyle.Important);
        }

        /// <summary>
        /// 工厂方法：获得唯一实例
        /// </summary>
        /// <returns>导演类唯一实例</returns>
        public static Director GetInstance()
        {
            return null == Director.synObject ? Director.synObject = new Director() : Director.synObject;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        private Director()
        {
            this.ResMana = ResourceManager.GetInstance();
            this.RunMana = new RuntimeManager();
            this.updateRender = new UpdateRender();
            this.updateRender.SetRuntimeManagerReference(this.RunMana);
            this.RunMana.SetScreenManager(ScreenManager.GetInstance());
            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromMilliseconds(GlobalDataContainer.DirectorTimerInterval);
            this.timer.Tick += UpdateContext;
#if NOTIME
#else
            this.timer.Start();
            this.InitRuntime();
#endif
        }

        /// <summary>
        /// 消息循环计时器
        /// </summary>
        private DispatcherTimer timer;

        /// <summary>
        /// 运行时环境
        /// </summary>
        private RuntimeManager RunMana;

        /// <summary>
        /// 资源管理器
        /// </summary>
        private ResourceManager ResMana;

        /// <summary>
        /// 画面刷新器
        /// </summary>
        private UpdateRender updateRender;

        /// <summary>
        /// 主窗体引用
        /// </summary>
        private MainWindow mwReference;

        /// <summary>
        /// 唯一实例
        /// </summary>
        private static Director synObject = null;
        #endregion
    }
}