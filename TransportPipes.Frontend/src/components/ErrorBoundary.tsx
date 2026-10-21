import { Component, type ErrorInfo, type ReactNode } from "react";

/** Keeps a broken panel from taking the whole game down: it is hidden and the error is logged */
export class ErrorBoundary extends Component<{ children: ReactNode; name: string }, { failed: boolean }> {
  state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error(`The ${this.props.name} failed`, error, info.componentStack);
    // Try again with the next update
    setTimeout(() => this.setState({ failed: false }), 3000);
  }

  render() {
    return this.state.failed ? null : this.props.children;
  }
}
