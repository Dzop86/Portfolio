import { TestBed } from '@angular/core/testing';
import { BarChart } from '../src/app/charts/bar-chart';
import { LineChart } from '../src/app/charts/line-chart';

describe('BarChart', () => {
  it('draws one bar per value, the longest for the largest, each with its text', async () => {
    const fixture = TestBed.createComponent(BarChart);
    fixture.componentRef.setInput('labelledBy', 'h');
    fixture.componentRef.setInput('bars', [
      { id: 'a', label: 'A', value: 100, text: '100 ms' },
      { id: 'b', label: 'B', value: 25, text: '25 ms' },
    ]);
    await fixture.whenStable();
    const el = fixture.nativeElement as HTMLElement;
    const width = (id: string) => Number(el.querySelector(`[data-bar="${id}"] rect`)!.getAttribute('width'));
    expect(width('a')).toBe(340); // 720 - 230 - 150
    expect(width('b')).toBe(85);
    expect(el.querySelector('[data-bar="b"]')!.textContent).toContain('25 ms');
    expect(el.querySelector('svg')!.getAttribute('viewBox')).toBe('0 0 720 92');
  });
});

describe('LineChart', () => {
  async function chart(series: { id: string; label: string; points: { x: number; y: number }[] }[], log = false) {
    const fixture = TestBed.createComponent(LineChart);
    const set = (name: string, value: unknown) => fixture.componentRef.setInput(name, value);
    set('labelledBy', 'h');
    set('xLabel', 'x');
    set('yLabel', 'y');
    set('formatX', (x: number) => String(x));
    set('formatY', (y: number) => String(y));
    set('log', log);
    set('series', series);
    await fixture.whenStable();
    return fixture.nativeElement as HTMLElement;
  }

  it('draws each series with its dots and names it at its end', async () => {
    const el = await chart([
      { id: 'obj', label: 'OBJ', points: [{ x: 10, y: 1 }, { x: 100, y: 10 }] },
      { id: 'ply', label: 'PLY', points: [{ x: 10, y: 2 }, { x: 100, y: 20 }] },
    ], true);
    expect(el.querySelectorAll('[data-series]')).toHaveLength(2);
    expect(el.querySelectorAll('[data-series="obj"] circle')).toHaveLength(2);
    expect([...el.querySelectorAll('.chart-label')].map((t) => t.textContent)).toEqual(['PLY', 'OBJ']);
    // Log scale over 10 to 100 on x: ticks 10, 20, 50, 100.
    const xTicks = [...el.querySelectorAll('text.chart-tick[text-anchor="middle"]')].map((t) => t.textContent);
    expect(xTicks).toEqual(['10', '20', '50', '100']);
  });

  it('draws nothing without points', async () => {
    const el = await chart([]);
    expect(el.querySelector('svg')).toBeNull();
  });
});
