using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using VRBuilder.Core;
using VRBuilder.Editor.UndoRedo;
using static UnityEditor.TypeCache;

namespace VRBuilder.Editor.UI.Graphics
{
    public class ProcessGraphView : GraphView
    {
        private Vector2 defaultNodeSize = new Vector2(200, 300);
        private IChapter currentChapter;
        public ProcessGraphNode EntryNode { get; private set; }

        public ProcessGraphView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("ProcessGraph"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            GridBackground grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            EntryNode = CreateEntryPointNode();
            AddElement(EntryNode);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            TypeCollection types = GetTypesDerivedFrom<ProcessGraphNode>();
            foreach (Type type in types)
            {
                evt.menu.AppendAction($"Create Node/{type.Name}", (status) => {
                    IStep step = EntityFactory.CreateStep("New Step");
                    step.StepMetadata.Position = status.eventInfo.mousePosition;
                    currentChapter.Data.Steps.Add(step);                    
                    // TODO support undo
                    GlobalEditorHandler.CurrentStepModified(step);
                });
            }

            evt.menu.AppendSeparator();

            base.BuildContextualMenu(evt);
        }

        public void SetChapter(IChapter chapter)
        {
            currentChapter = chapter;

            IDictionary<IStep, StepGraphNode> stepNodes = SetupSteps(currentChapter);

            foreach (IStep step in stepNodes.Keys)
            {
                StepGraphNode node = stepNodes[step];
                AddElement(node);
            }

            SetupTransitions(currentChapter, EntryNode, stepNodes);
        }      

        private void LinkNodes(Port output, Port input)
        {
            Edge edge = new Edge
            {
                output = output,
                input = input,
            };

            edge.input.Connect(edge);
            edge.output.Connect(edge);
            Add(edge);

            UpdateOutputPortName(output);
        }

        private IDictionary<IStep, StepGraphNode> SetupSteps(IChapter chapter)
        {
            return chapter.Data.Steps.OrderBy(step => step == chapter.ChapterMetadata.LastSelectedStep).ToDictionary(step => step, CreateStepNode);
        }

        private void SetupTransitions(IChapter chapter, ProcessGraphNode entryNode, IDictionary<IStep, StepGraphNode> stepNodes)
        {
            if (chapter.Data.FirstStep != null)
            {
                LinkNodes(EntryNode.outputContainer[0].Query<Port>(), stepNodes[chapter.Data.FirstStep].inputContainer[0].Query<Port>());
            }

            foreach (IStep step in stepNodes.Keys)
            {
                foreach (ITransition transition in step.Data.Transitions.Data.Transitions)
                {
                    Port outputPort = AddTransitionPort(stepNodes[step]);

                    if (transition.Data.TargetStep != null)
                    {
                        ProcessGraphNode target = stepNodes[transition.Data.TargetStep];
                        LinkNodes(outputPort, target.inputContainer[0].Query<Port>());
                    }
                }
            }
        }

        private ProcessGraphNode CreateEntryPointNode()
        {
            ProcessGraphNode node = new ProcessGraphNode
            {
                title = "Start",
                GUID = Guid.NewGuid().ToString(),
                IsEntryPoint = true,                
            };

            Port transitionPort = CreatePort(node, Direction.Output);
            transitionPort.portName = "Next";
            node.outputContainer.Add(transitionPort);         

            node.RefreshExpandedState();
            node.RefreshPorts();

            node.SetPosition(new Rect(100, 200, 100, 150));
            return node;
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();
            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node && startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }

        public Port AddTransitionPort(ProcessGraphNode node)
        {
            Port port = CreatePort(node, Direction.Output);
            EdgeConnectorListener connectorListener = new EdgeConnectorListener();
            connectorListener.ConnectorDroppedOnPort += OnConnectorDroppedOnPort;
            port.AddManipulator(new EdgeConnector<Edge>(connectorListener));
            //int outputPortCount = node.outputContainer.Query("connector").ToList().Count;
            UpdateOutputPortName(port);

            node.outputContainer.Add(port);
            node.RefreshExpandedState();
            node.RefreshPorts();

            return port;
        }

        private void OnConnectorDroppedOnPort(object sender, EdgeConnectorListenerEventArgs e)
        {
            StepGraphNode originNode = e.Edge.output.node as StepGraphNode;
            StepGraphNode targetNode = e.Edge.input.node as StepGraphNode;

            if (originNode == null || targetNode == null)
            {
                Debug.LogError("Connected non-step node");
                return;
            }

            ITransition transition = originNode.Step.Data.Transitions.Data.Transitions[originNode.outputContainer.IndexOf(e.Edge.output)];
            transition.Data.TargetStep = targetNode.Step;
            UpdateOutputPortName(e.Edge.output);
        }

        private void UpdateOutputPortName(Port port)
        {
            if(port.connected == false)
            {
                port.portName = "End Chapter";
                return;
            }

            Port destination = port.edgeConnector.target as Port;

            if (destination != null)
            {
                port.portName = $"To {destination.node.title}";
            }
            else
            {
                port.portName = "Connected";
            }
        }

        internal void CreateTransition(StepGraphNode node)
        {
            ITransition transition = EntityFactory.CreateTransition();

            RevertableChangesHandler.Do(new ProcessCommand(
                () =>
                {
                    node.Step.Data.Transitions.Data.Transitions.Add(transition);
                    AddTransitionPort(node);
                },
                () =>
                {
                    // TODO
                    node.Step.Data.Transitions.Data.Transitions.Remove(transition);
                }
            ));            
        }

        internal StepGraphNode CreateStepNode(IStep step)
        {
            StepGraphNode node = new StepGraphNode
            {
                title = step.Data.Name,
                GUID = Guid.NewGuid().ToString(),
                Step = step,
            };

            Port inputPort = CreatePort(node, Direction.Input, Port.Capacity.Multi);
            inputPort.portName = "Input";
            node.inputContainer.Add(inputPort);

            Button addTransitionButton = new Button(() => { CreateTransition(node); });
            addTransitionButton.text = "New Transition";
            node.titleContainer.Add(addTransitionButton);

            node.SetPosition(new Rect(node.Step.StepMetadata.Position, defaultNodeSize));
            node.RefreshExpandedState();
            node.RefreshPorts();

            return node;
        }        

        private Port CreatePort(ProcessGraphNode node, Direction direction, Port.Capacity capacity = Port.Capacity.Single)
        {
            return node.InstantiatePort(Orientation.Horizontal, direction, capacity, typeof(ProcessExec));
        }
    }
}
